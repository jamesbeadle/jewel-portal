using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// The heart of the posting model (scope §6): approval resolves the worker's rate effective on
// the worked date, snapshots rate + cost onto the timesheet, and enforces the budget
// hard-block (workflow 07-D). Partial success — approvable timesheets approve in one atomic
// SaveChanges; blocked ones come back with reasons. Only approved time is actual cost.

public sealed class ApproveTimesheetsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ApproveTimesheetsHandler handler;
    public ApproveTimesheetsEndpoint(SignedInUserResolver users, ApproveTimesheetsHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ApproveTimesheets))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/labour/approvals")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ApproveTimesheets.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var body = await request.ReadFromJsonAsync<ApproveTimesheets>();
        if (body is null || body.TimesheetIds is null || body.TimesheetIds.Count == 0)
            return new BadRequestObjectResult(new[] { "At least one timesheet is required." });
        if (body.AllowOverBudget)
        {
            // The deliberate exception to the hard-block: MD/FD/Admin only, and never silently —
            // a reason is part of the command, because the audit row is the point of the feature.
            if (!LabourRoleSets.OverrideBudgetBlock.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
            if (string.IsNullOrWhiteSpace(body.OverBudgetReason))
                return new BadRequestObjectResult(new[] { "An over-budget approval needs a reason." });
        }
        var command = body with { ProjectId = projectId };
        return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, request.HttpContext.RequestAborted));
    }
}

public sealed class ApproveTimesheetsHandler : ICommandHandler<ApproveTimesheets, LabourApprovalResult>
{
    private readonly JpmsContext context;
    private readonly Audit.AuditTrail audit;
    public ApproveTimesheetsHandler(JpmsContext context, Audit.AuditTrail audit)
    { this.context = context; this.audit = audit; }

    public Task<LabourApprovalResult> HandleAsync(ApproveTimesheets command, CancellationToken cancellationToken) =>
        HandleAsync(command, approvedByEmail: "", cancellationToken);

    public async Task<LabourApprovalResult> HandleAsync(ApproveTimesheets command, string approvedByEmail, CancellationToken cancellationToken)
    {
        var timesheets = await context.Timesheets
            .Where(timesheet => timesheet.ProjectId == command.ProjectId
                                && command.TimesheetIds.Contains(timesheet.TimesheetId))
            .OrderBy(timesheet => timesheet.WorkedOn)
            .ToListAsync(cancellationToken);

        var workerIds = timesheets.Select(timesheet => timesheet.WorkerId).Where(id => id != "").Distinct().ToList();
        var workers = await context.Workers
            .Where(worker => workerIds.Contains(worker.WorkerId))
            .ToDictionaryAsync(worker => worker.WorkerId, cancellationToken);
        var rateHistory = await context.WorkerRateHistories
            .Where(history => workerIds.Contains(history.WorkerId))
            .OrderBy(history => history.EffectiveFrom)
            .ToListAsync(cancellationToken);

        var budgets = await context.CostCodeBudgets
            .Where(budget => budget.ProjectId == command.ProjectId)
            .ToDictionaryAsync(budget => budget.CostCode, StringComparer.OrdinalIgnoreCase, cancellationToken);

        // Labour already approved per cost code counts against remaining budget, and each
        // approval in this batch accumulates so the batch can't jointly bust a budget that each
        // item individually squeezes past.
        var approvedLabourRows = await context.Timesheets
            .Where(timesheet => timesheet.ProjectId == command.ProjectId
                                && timesheet.Status == (int)TimesheetStatus.Approved)
            .GroupBy(timesheet => timesheet.CostCode)
            .Select(group => new { CostCode = group.Key, Amount = group.Sum(timesheet => timesheet.CostAmount) })
            .ToListAsync(cancellationToken);
        var labourByCode = approvedLabourRows.ToDictionary(
            row => row.CostCode, row => row.Amount, StringComparer.OrdinalIgnoreCase);

        var approved = new List<Data.Entities.TimesheetEntity>();
        var overridden = new List<(Data.Entities.TimesheetEntity Timesheet, string BlockReason)>();
        var failures = new List<LabourApprovalFailure>();
        var requestedIds = command.TimesheetIds.ToHashSet();
        foreach (var missingId in requestedIds.Where(id => timesheets.All(timesheet => timesheet.TimesheetId != id)))
            failures.Add(new LabourApprovalFailure(missingId, "Timesheet not found on this project."));

        foreach (var timesheet in timesheets)
        {
            if (timesheet.Status == (int)TimesheetStatus.Approved)
            { failures.Add(new LabourApprovalFailure(timesheet.TimesheetId, "Already approved.")); continue; }
            if (timesheet.Status == (int)TimesheetStatus.Rejected)
            { failures.Add(new LabourApprovalFailure(timesheet.TimesheetId, "Rejected — the worker must resubmit first.")); continue; }
            if (timesheet.WorkerId == "" || !workers.TryGetValue(timesheet.WorkerId, out var worker))
            { failures.Add(new LabourApprovalFailure(timesheet.TimesheetId, "No worker record — legacy timesheets need a worker before costed approval.")); continue; }
            // Weekly entries arrive uncoded on purpose (the accountant records WHERE, the approver
            // codes WHAT). Say so plainly — the "" budget lookup below would refuse it anyway, but
            // with a message about budgets that sends the approver hunting in the wrong place.
            if (string.IsNullOrWhiteSpace(timesheet.CostCode))
            { failures.Add(new LabourApprovalFailure(timesheet.TimesheetId, $"{timesheet.WorkedOn:ddd dd MMM} has no cost code yet — Adjust the row to code it, then approve.")); continue; }

            var history = rateHistory
                .Where(row => row.WorkerId == timesheet.WorkerId)
                .Select(row => (row.EffectiveFrom, row.HourlyRate))
                .ToList();
            var rate = LabourRules.ResolveRate(history, worker.HourlyRate, timesheet.WorkedOn);
            var cost = LabourRules.CostOf(timesheet.Hours, rate);

            var budget = budgets.TryGetValue(timesheet.CostCode, out var found)
                ? ((decimal, decimal, decimal)?)(found.AllocatedAmount, found.SpentAmount, found.CommittedAmount)
                : null;
            var alreadyApproved = labourByCode.TryGetValue(timesheet.CostCode, out var sum) ? sum : 0m;
            var blockReason = LabourRules.BudgetBlockReason(timesheet.CostCode, cost, budget, alreadyApproved);
            if (blockReason is not null)
            {
                // The hard-block stands unless this command is a deliberate, endpoint-gated
                // override (MD/FD/Admin with a reason) — then the row approves anyway and the
                // override is written to the audit trail after the save. BudgetBlocked marks the
                // failure so the Labour tab can offer that follow-up on exactly these rows.
                if (!command.AllowOverBudget)
                { failures.Add(new LabourApprovalFailure(timesheet.TimesheetId, blockReason, BudgetBlocked: true)); continue; }
                overridden.Add((timesheet, blockReason));
            }

            timesheet.RateApplied = rate;
            timesheet.CostAmount = cost;
            timesheet.Status = (int)TimesheetStatus.Approved;
            timesheet.IsApproved = true;
            timesheet.ApprovedByEmail = approvedByEmail;
            timesheet.ApprovedAt = DateTimeOffset.UtcNow;
            timesheet.RejectionReason = "";
            labourByCode[timesheet.CostCode] = alreadyApproved + cost;
            approved.Add(timesheet);
        }

        await context.SaveChangesAsync(cancellationToken);

        // Each override on the audit trail — after the save, best-effort, per the AuditTrail
        // convention. One row per timesheet: the trail is the only place the fact that an
        // approval was over budget survives once the status just reads Approved.
        foreach (var (timesheet, blockReason) in overridden)
            await audit.WriteAsync(
                AuditEventType.LabourBudgetOverridden,
                $"{workers[timesheet.WorkerId].Name} {timesheet.WorkedOn:ddd dd MMM} "
                + $"(£{timesheet.CostAmount:N2} against {timesheet.CostCode}) approved over budget — {blockReason} "
                + $"Reason given: {command.OverBudgetReason}",
                projectId: command.ProjectId,
                actorEmail: approvedByEmail,
                cancellationToken: cancellationToken);

        var approvedModels = approved.Select(timesheet =>
            timesheet.ToDetail(workers[timesheet.WorkerId].Name)).ToList();
        return new LabourApprovalResult(approvedModels, failures);
    }
}
