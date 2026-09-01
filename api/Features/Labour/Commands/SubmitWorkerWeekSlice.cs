using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Labour;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// The accountant's weekly entry (Labour overview → "Enter a week"): one worker's whole week of
// site days in one command, each landing as an ordinary Submitted timesheet on its own project.
// Days that already carry a timesheet or a recorded absence are SKIPPED with a per-day reason,
// never overwritten — corrections belong to the project's Labour tab (adjust/reject), and the
// worker's own My day submissions must not be silently clobbered by a transcription.

public sealed class SubmitWorkerWeekEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SubmitWorkerWeekHandler handler;
    public SubmitWorkerWeekEndpoint(SignedInUserResolver users, SubmitWorkerWeekHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(SubmitWorkerWeek))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/weeks/timesheets")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ApproveTimesheets.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var command = await request.ReadFromJsonAsync<SubmitWorkerWeek>();
        if (command is null || string.IsNullOrWhiteSpace(command.WorkerId)) return new BadRequestResult();
        if (command.Days is not { Count: > 0 })
            return new BadRequestObjectResult(new[] { "At least one day with a site is required." });

        // Whole-command validation up front: a half-landed week the caller has to diff against
        // their form is worse than a clean refusal naming the bad day.
        var weekStart = SiteClock.WorkDateOf(command.WeekStart);
        foreach (var day in command.Days)
        {
            var date = SiteClock.WorkDateOf(day.Date);
            if (date < weekStart || date >= weekStart.AddDays(7))
                return new BadRequestObjectResult(new[] { $"{date:ddd dd MMM} is not in the week commencing {weekStart:dd MMM}." });
            if (string.IsNullOrWhiteSpace(day.ProjectId))
                return new BadRequestObjectResult(new[] { $"{date:ddd dd MMM} needs a site." });
            if (!LabourRules.IsValidHours(day.Hours))
                return new BadRequestObjectResult(new[] { $"{date:ddd dd MMM}: hours must be in half-hour steps of at least 0.5." });
            // No cost-code requirement here, deliberately (decision 2026-08-21): the accountant
            // transcribes WHERE people were; the MD codes the day when he approves it. An uncoded
            // day cannot be approved until it is coded — ApproveTimesheets says so per row.
        }
        if (command.Days.GroupBy(day => SiteClock.WorkDateOf(day.Date)).Any(group => group.Count() > 1))
            return new BadRequestObjectResult(new[] { "Each day can only appear once in the week." });

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException rejection)
        {
            return new BadRequestObjectResult(new[] { rejection.Message });
        }
    }
}

public sealed class SubmitWorkerWeekHandler : ICommandHandler<SubmitWorkerWeek, WorkerWeekResult>
{
    private readonly JpmsContext context;
    public SubmitWorkerWeekHandler(JpmsContext context) { this.context = context; }

    public async Task<WorkerWeekResult> HandleAsync(SubmitWorkerWeek command, CancellationToken cancellationToken)
    {
        var worker = await context.Workers.FindAsync(new object[] { command.WorkerId }, cancellationToken)
            ?? throw new InvalidOperationException($"Worker {command.WorkerId} not found.");

        var dates = command.Days.Select(day => SiteClock.WorkDateOf(day.Date)).ToList();

        // What the week already holds — a day with ANY timesheet (whoever entered it, whatever
        // its status) or a recorded absence is already explained and stays untouched.
        var existingSheets = await context.Timesheets.AsNoTracking()
            .Where(sheet => sheet.WorkerId == command.WorkerId && dates.Contains(sheet.WorkedOn))
            .ToListAsync(cancellationToken);
        var existingAbsences = await context.WorkerAbsences.AsNoTracking()
            .Where(absence => absence.WorkerId == command.WorkerId && dates.Contains(absence.Date))
            .ToListAsync(cancellationToken);

        var submittedProjectIds = command.Days.Select(day => day.ProjectId).Distinct().ToList();
        var namedProjectIds = submittedProjectIds
            .Concat(existingSheets.Select(sheet => sheet.ProjectId)).Distinct().ToList();
        var projectNames = await context.Projects.AsNoTracking()
            .Where(project => namedProjectIds.Contains(project.ProjectId))
            .ToDictionaryAsync(project => project.ProjectId, project => project.Name, cancellationToken);
        var missingProject = submittedProjectIds.FirstOrDefault(id => !projectNames.ContainsKey(id));
        if (missingProject is not null)
            throw new InvalidOperationException($"Project {missingProject} not found.");

        var outcomes = new List<WorkerWeekDayOutcome>();
        foreach (var day in command.Days.OrderBy(day => day.Date))
        {
            var date = SiteClock.WorkDateOf(day.Date);

            var recorded = existingSheets.FirstOrDefault(sheet => sheet.WorkedOn == date);
            if (recorded is not null)
            {
                var siteName = projectNames.TryGetValue(recorded.ProjectId, out var name) ? name : recorded.ProjectId;
                outcomes.Add(new WorkerWeekDayOutcome(date, false,
                    $"already recorded — {siteName}, {recorded.Hours:0.#} h ({(TimesheetStatus)recorded.Status})"));
                continue;
            }

            var absence = existingAbsences.FirstOrDefault(row => row.Date == date);
            if (absence is not null)
            {
                outcomes.Add(new WorkerWeekDayOutcome(date, false,
                    $"absence already recorded ({(AbsenceKind)absence.Kind})"));
                continue;
            }

            context.Timesheets.Add(new TimesheetEntity
            {
                TimesheetId = CommercialIdentifierFactory.NextTimesheetId(),
                ProjectId = day.ProjectId,
                PersonEmail = worker.ContactEmail,
                WorkerId = worker.WorkerId,
                WorkedOn = date,
                Hours = day.Hours,
                CostCode = day.CostCode?.Trim() ?? "",
                Status = (int)TimesheetStatus.Submitted,
                IsApproved = false,
            });
            outcomes.Add(new WorkerWeekDayOutcome(date, true,
                $"{projectNames[day.ProjectId]}, {day.Hours:0.#} h — submitted for approval"));
        }

        await context.SaveChangesAsync(cancellationToken);
        return new WorkerWeekResult(worker.WorkerId, worker.Name, outcomes);
    }
}
