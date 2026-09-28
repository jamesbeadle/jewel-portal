using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>
/// POST /api/my/labour/sign-out — the worker's day, logged once: one Submitted timesheet per cost
/// code, the attendance closed at the sign-out time, and the day's note written onto the project's
/// progress feed in the worker's own name, in one save. One sign-out per project per day.
/// </summary>
public sealed class MySiteSignOutEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly MySiteSignOutHandler handler;
    public MySiteSignOutEndpoint(SignedInUserResolver users, MySiteSignOutHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(MySiteSignOut))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "my/labour/sign-out")] HttpRequest request)
    {
        var httpContext = request.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.LogOwnTime.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<MySiteSignOut>(cancellationToken);
        if (command is null || string.IsNullOrWhiteSpace(command.ProjectId) || command.Entries is null) return new BadRequestResult();
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, cancellationToken));
        }
        catch (InvalidOperationException rejection)
        {
            return new BadRequestObjectResult(new[] { rejection.Message });
        }
    }
}

public sealed class MySiteSignOutHandler : ICommandHandler<MySiteSignOut, MySiteDayLogged>
{
    private readonly JpmsContext context;
    private readonly MyDayCostCodes costCodes;
    private readonly MyDayRaisedRecords raisedRecords;
    public MySiteSignOutHandler(JpmsContext context, MyDayCostCodes costCodes, MyDayRaisedRecords raisedRecords)
    { this.context = context; this.costCodes = costCodes; this.raisedRecords = raisedRecords; }

    public Task<MySiteDayLogged> HandleAsync(MySiteSignOut command, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("MySiteSignOut requires the signed-in email — use the endpoint.");

    public async Task<MySiteDayLogged> HandleAsync(MySiteSignOut command, string email, CancellationToken cancellationToken)
    {
        var worker = await WorkerByEmail.ResolveAsync(context, email, cancellationToken);
        var today = SiteClock.Today();
        var attendance = await OpenAttendanceAsync(command.ProjectId, worker, today, cancellationToken);
        await CheckAsync(command, cancellationToken);
        var signedOutAt = MyDayMoments.Resolve(command.SignedOutAt, today, "sign-out");
        if (signedOutAt < attendance.SignedInAt)
            throw new InvalidOperationException("The sign-out time cannot be before the sign-in time.");

        foreach (var entry in command.Entries)
            context.Timesheets.Add(MyDayTimesheets.Submitted(command.ProjectId, entry, worker, attendance, email, today));
        var note = ProgressUpdateRows.New(
            ProgressIdentifierFactory.NextProgressUpdateId(), command.ProjectId, MyDayNotes.LogTitle(worker),
            command.Description, today, null, email, DateTimeOffset.UtcNow);
        context.ProgressUpdates.Add(note);
        var instructionReference = await raisedRecords.RaiseInstructionAsync(command.Instruction, note, email, cancellationToken);
        var defectReference = await raisedRecords.RaiseDefectAsync(command.Defect, note, email, cancellationToken);
        attendance.SignedOutAt = signedOutAt;
        await context.SaveChangesAsync(cancellationToken);
        return new MySiteDayLogged(attendance.SiteAttendanceId, note.ProgressUpdateId, instructionReference, defectReference);
    }

    private async Task CheckAsync(MySiteSignOut command, CancellationToken cancellationToken)
    {
        var allowedCodes = await costCodes.AllowedForAsync(command.ProjectId, cancellationToken);
        var errors = LabourRules.CheckSignOutEntries(command.Entries, allowedCodes).ToList();
        if (!MyDayNotes.IsGiven(command.Description))
            errors.Add("Say what was done today — the words are the day's record.");
        errors.AddRange(SiteLogRules.CheckInstruction(command.Instruction));
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(" ", errors));
    }

    private async Task<SiteAttendanceEntity> OpenAttendanceAsync(string projectId, WorkerEntity worker, DateTimeOffset today, CancellationToken cancellationToken)
    {
        var attendance = await context.SiteAttendances.FirstOrDefaultAsync(
            row => row.ProjectId == projectId && row.WorkerId == worker.WorkerId && row.WorkDate == today, cancellationToken)
            ?? throw new InvalidOperationException("You haven't signed in today — sign in first.");
        if (attendance.SignedOutAt is not null)
            throw new InvalidOperationException("You've already signed out today. Contact your Project Manager if you need to amend your hours.");
        return attendance;
    }
}
