using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>
/// POST /api/my/labour/missed-day — a day the worker was on site but never logged, filled in from
/// My day's week after the date: one Submitted timesheet marked as filed late and the day's note
/// on the project's progress feed, in one save. No attendance is written — there was no sign-in
/// at the gate to record. The office approves it like any other day.
/// </summary>
public sealed class MyLogMissedSiteDayEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly MyLogMissedSiteDayHandler handler;
    public MyLogMissedSiteDayEndpoint(SignedInUserResolver users, MyLogMissedSiteDayHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(MyLogMissedSiteDay))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "my/labour/missed-day")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.LogOwnTime.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<MyLogMissedSiteDay>(cancellationToken);
        if (command is null || string.IsNullOrWhiteSpace(command.ProjectId)) return new BadRequestResult();
        try { return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, cancellationToken)); }
        catch (InvalidOperationException rejection) { return new BadRequestObjectResult(new[] { rejection.Message }); }
    }
}

public sealed class MyLogMissedSiteDayHandler : ICommandHandler<MyLogMissedSiteDay, MySiteDayLogged>
{
    private readonly JpmsContext context;
    private readonly MyDayCostCodes costCodes;
    public MyLogMissedSiteDayHandler(JpmsContext context, MyDayCostCodes costCodes) { this.context = context; this.costCodes = costCodes; }

    public Task<MySiteDayLogged> HandleAsync(MyLogMissedSiteDay command, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("MyLogMissedSiteDay requires the signed-in email — use the endpoint.");

    public async Task<MySiteDayLogged> HandleAsync(MyLogMissedSiteDay command, string email, CancellationToken cancellationToken)
    {
        var worker = await WorkerByEmail.ResolveAsync(context, email, cancellationToken);
        await MyDayAssignment.EnsureAssignedAsync(context, command.ProjectId, worker, cancellationToken);
        var workDate = MyDayFiling.PastWorkDate(SiteClock.WorkDateOf(command.Date), SiteClock.Today());
        await CheckAsync(command, cancellationToken);
        await WorkerWeekLock.EnsureOpenAsync(context, worker.WorkerId, workDate, cancellationToken);
        await MyDayVacancy.EnsureAsync(context, command.ProjectId, worker, email, workDate, cancellationToken);

        var entry = new SiteSignOutEntry(command.CostCode, command.Hours);
        var timesheet = MyDayTimesheets.FiledLate(command.ProjectId, entry, worker, email, workDate);
        context.Timesheets.Add(timesheet);
        var note = ProgressUpdateRows.New(
            ProgressIdentifierFactory.NextProgressUpdateId(), command.ProjectId, MyDayNotes.LogTitle(worker),
            command.Description, workDate, null, email, DateTimeOffset.UtcNow);
        context.ProgressUpdates.Add(note);
        await context.SaveChangesAsync(cancellationToken);
        return new MySiteDayLogged("", note.ProgressUpdateId);
    }

    private async Task CheckAsync(MyLogMissedSiteDay command, CancellationToken cancellationToken)
    {
        var allowedCodes = await costCodes.AllowedForAsync(command.ProjectId, cancellationToken);
        var entry = new SiteSignOutEntry(command.CostCode, command.Hours);
        var errors = LabourRules.CheckSignOutEntries(new[] { entry }, allowedCodes).ToList();
        if (!MyDayNotes.IsGiven(command.Description)) errors.Add("Say what was done that day — the words are the day's record.");
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(" ", errors));
    }
}
