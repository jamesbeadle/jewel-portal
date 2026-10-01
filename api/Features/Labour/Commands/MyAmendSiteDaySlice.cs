using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>
/// POST /api/my/labour/amend — a day the worker logged, changed by the worker while the office has
/// not approved it: the timesheet's hours and cost code, the note's words, the sign-out time and
/// the site it belongs on, in one save. An approved day is the Project Manager's to change.
/// </summary>
public sealed class MyAmendSiteDayEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly MyAmendSiteDayHandler handler;
    public MyAmendSiteDayEndpoint(SignedInUserResolver users, MyAmendSiteDayHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(MyAmendSiteDay))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "my/labour/amend")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.LogOwnTime.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<MyAmendSiteDay>(cancellationToken);
        if (command is null || string.IsNullOrWhiteSpace(command.TimesheetId)) return new BadRequestResult();
        try { return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, cancellationToken)); }
        catch (InvalidOperationException rejection) { return new BadRequestObjectResult(new[] { rejection.Message }); }
    }
}

public sealed class MyAmendSiteDayHandler : ICommandHandler<MyAmendSiteDay, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly MyDayCostCodes costCodes;
    public MyAmendSiteDayHandler(JpmsContext context, MyDayCostCodes costCodes) { this.context = context; this.costCodes = costCodes; }

    public Task<Acknowledgement> HandleAsync(MyAmendSiteDay command, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("MyAmendSiteDay requires the signed-in email — use the endpoint.");

    public async Task<Acknowledgement> HandleAsync(MyAmendSiteDay command, string email, CancellationToken cancellationToken)
    {
        var worker = await WorkerByEmail.ResolveAsync(context, email, cancellationToken);
        var timesheet = await OwnOpenTimesheetAsync(command.TimesheetId, worker, cancellationToken);
        await WorkerWeekLock.EnsureOpenAsync(context, worker.WorkerId, timesheet.WorkedOn, cancellationToken);
        var projectId = await MyDaySiteMove.DestinationAsync(context, worker, timesheet.ProjectId, command.ProjectId, timesheet.WorkedOn, cancellationToken);
        await CheckAsync(command, projectId, cancellationToken);
        var note = await OwnNoteAsync(timesheet, email, cancellationToken);
        var attendance = await context.SiteAttendances.FirstOrDefaultAsync(row => row.SiteAttendanceId == timesheet.SiteAttendanceId, cancellationToken);

        timesheet.Hours = command.Hours;
        timesheet.CostCode = command.CostCode;
        timesheet.Status = (int)TimesheetStatus.Submitted;
        timesheet.RejectionReason = "";
        timesheet.IsFiledLate = timesheet.IsFiledLate || MyDayFiling.IsLate(timesheet.WorkedOn, SiteClock.Today());
        note.Description = command.Description.Trim();
        MyDaySiteMove.Move(timesheet, note, attendance, projectId);
        if (attendance is not null) attendance.SignedOutAt = SignedOutAt(command, timesheet, attendance);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(timesheet.TimesheetId);
    }

    private async Task<TimesheetEntity> OwnOpenTimesheetAsync(string timesheetId, WorkerEntity worker, CancellationToken cancellationToken)
    {
        var timesheet = await context.Timesheets.FirstOrDefaultAsync(row => row.TimesheetId == timesheetId, cancellationToken)
            ?? throw new InvalidOperationException("That day is no longer here.");
        if (timesheet.WorkerId != worker.WorkerId) throw new InvalidOperationException("This day isn't yours to amend.");
        if (timesheet.Status == (int)TimesheetStatus.Approved)
            throw new InvalidOperationException("The office has approved this day — ask your Project Manager to change it.");
        return timesheet;
    }

    private async Task CheckAsync(MyAmendSiteDay command, string projectId, CancellationToken cancellationToken)
    {
        var allowedCodes = await costCodes.AllowedForAsync(projectId, cancellationToken);
        var entry = new SiteSignOutEntry(command.CostCode, command.Hours);
        var errors = LabourRules.CheckSignOutEntries(new[] { entry }, allowedCodes).ToList();
        if (!MyDayNotes.IsGiven(command.Description)) errors.Add("Say what was done that day — the words are the day's record.");
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(" ", errors));
    }

    private async Task<ProgressUpdateEntity> OwnNoteAsync(TimesheetEntity timesheet, string email, CancellationToken cancellationToken) =>
        await context.ProgressUpdates
            .Where(note => note.ProjectId == timesheet.ProjectId && note.CreatedByEmail == email && note.WorkDate == timesheet.WorkedOn)
            .OrderByDescending(note => note.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new InvalidOperationException("That day's note is no longer here — ask your Project Manager.");

    private static DateTimeOffset SignedOutAt(MyAmendSiteDay command, TimesheetEntity timesheet, SiteAttendanceEntity attendance)
    {
        if (command.SignedOutAt is null) return attendance.SignedOutAt ?? DateTimeOffset.UtcNow;
        var signedOutAt = MyDayMoments.Resolve(command.SignedOutAt, timesheet.WorkedOn, "sign-out");
        if (signedOutAt < attendance.SignedInAt) throw new InvalidOperationException("The sign-out time cannot be before the sign-in time.");
        return signedOutAt;
    }
}
