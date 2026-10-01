using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>POST /api/labour/plan — the planner's one tap: a worker's days marked not in (time off
/// asked for, recorded as the office's absence) or back in (the absence removed). A day already
/// logged is refused for not in — the worker was there.</summary>
public sealed class PlanWorkerDaysEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PlanWorkerDaysHandler handler;
    public PlanWorkerDaysEndpoint(SignedInUserResolver users, PlanWorkerDaysHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(PlanWorkerDays))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/plan")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageWorkers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<PlanWorkerDays>(cancellationToken);
        if (command is null || string.IsNullOrWhiteSpace(command.WorkerId) || command.Dates is not { Count: > 0 })
            return new BadRequestObjectResult(new[] { "Name the worker and at least one day." });
        try { return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, cancellationToken)); }
        catch (InvalidOperationException rejection) { return new BadRequestObjectResult(new[] { rejection.Message }); }
    }
}

public sealed class PlanWorkerDaysHandler : ICommandHandler<PlanWorkerDays, Acknowledgement>
{
    private const string PlannedNote = "Planned not in on the week planner.";
    private readonly JpmsContext context;
    public PlanWorkerDaysHandler(JpmsContext context) { this.context = context; }

    public Task<Acknowledgement> HandleAsync(PlanWorkerDays command, CancellationToken cancellationToken) =>
        HandleAsync(command, plannedByEmail: "", cancellationToken);

    public async Task<Acknowledgement> HandleAsync(PlanWorkerDays command, string plannedByEmail, CancellationToken cancellationToken)
    {
        var worker = await context.Workers.FirstOrDefaultAsync(row => row.WorkerId == command.WorkerId, cancellationToken)
            ?? throw new InvalidOperationException("That worker is no longer here.");
        var dates = command.Dates.Select(SiteClock.WorkDateOf).Distinct().ToList();
        var absences = await context.WorkerAbsences
            .Where(row => row.WorkerId == worker.WorkerId && dates.Contains(row.Date)).ToListAsync(cancellationToken);
        if (command.IsIn) context.WorkerAbsences.RemoveRange(absences);
        else await MarkNotInAsync(worker, dates, absences, plannedByEmail, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(worker.WorkerId);
    }

    private async Task MarkNotInAsync(
        WorkerEntity worker, List<DateTimeOffset> dates, List<WorkerAbsenceEntity> absences, string plannedByEmail, CancellationToken cancellationToken)
    {
        var loggedDay = await context.Timesheets.AsNoTracking()
            .Where(row => row.WorkerId == worker.WorkerId && dates.Contains(row.WorkedOn))
            .Select(row => (DateTimeOffset?)row.WorkedOn).FirstOrDefaultAsync(cancellationToken);
        if (loggedDay is { } logged)
            throw new InvalidOperationException($"{worker.Name} logged {logged:dddd d MMMM} — a day worked can't be planned off.");
        var recorded = absences.Select(row => row.Date).ToHashSet();
        foreach (var date in dates.Where(date => !recorded.Contains(date)))
            context.WorkerAbsences.Add(new WorkerAbsenceEntity
            {
                WorkerAbsenceId = LabourIdentifierFactory.NextWorkerAbsenceId(),
                WorkerId = worker.WorkerId,
                Date = date,
                Kind = (int)AbsenceKind.Holiday,
                Note = PlannedNote,
                RecordedByEmail = plannedByEmail,
                RecordedAt = DateTimeOffset.UtcNow,
            });
    }
}
