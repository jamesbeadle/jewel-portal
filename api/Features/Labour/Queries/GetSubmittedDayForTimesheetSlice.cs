using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>GET /api/labour/timesheets/{timesheetId}/day — what the worker submitted with the day,
/// read by the office from the Labour tab before it codes and approves the hours (Jeremy, 30 Sep
/// 2026: "a way to actually see what he has submitted").</summary>
public sealed class GetSubmittedDayForTimesheetEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly GetSubmittedDayForTimesheetHandler handler;
    public GetSubmittedDayForTimesheetEndpoint(SignedInUserResolver users, GetSubmittedDayForTimesheetHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(GetSubmittedDayForTimesheet))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "labour/timesheets/{timesheetId}/day")] HttpRequest request, string timesheetId)
    {
        var httpContext = request.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!JpmsRoleSets.AllInternal.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var record = await handler.HandleAsync(new GetSubmittedDayForTimesheet(timesheetId), cancellationToken);
        return record is null ? new NotFoundResult() : new OkObjectResult(record);
    }
}

/// <summary>The day's note is the worker's own progress update on that site and date; the
/// photographs are the note's; the times are the attendance the sign-out closed.</summary>
public sealed class GetSubmittedDayForTimesheetHandler : IQueryHandler<GetSubmittedDayForTimesheet, SubmittedDay?>
{
    private readonly JpmsContext context;
    public GetSubmittedDayForTimesheetHandler(JpmsContext context) { this.context = context; }

    public async Task<SubmittedDay?> HandleAsync(GetSubmittedDayForTimesheet query, CancellationToken cancellationToken)
    {
        var timesheet = await context.Timesheets.AsNoTracking().FirstOrDefaultAsync(row => row.TimesheetId == query.TimesheetId, cancellationToken);
        if (timesheet is null) return null;
        var worker = await context.Workers.AsNoTracking().FirstOrDefaultAsync(row => row.WorkerId == timesheet.WorkerId, cancellationToken);
        var note = await NoteForAsync(timesheet, worker, cancellationToken);
        var photos = await PhotosOfAsync(note, cancellationToken);
        var attendance = await context.SiteAttendances.AsNoTracking().FirstOrDefaultAsync(row => row.SiteAttendanceId == timesheet.SiteAttendanceId, cancellationToken);
        var raised = await RaisedByAsync(note, cancellationToken);
        return new SubmittedDay(
            timesheet.TimesheetId, worker?.Name ?? timesheet.PersonEmail, timesheet.WorkedOn, timesheet.ProjectId,
            note?.ProgressUpdateId ?? "", note?.Description ?? "", photos, attendance?.SignedInAt, attendance?.SignedOutAt,
            raised.SiteInstruction, raised.Defect, timesheet.IsFiledLate);
    }

    private async Task<ProgressUpdateEntity?> NoteForAsync(TimesheetEntity timesheet, WorkerEntity? worker, CancellationToken cancellationToken)
    {
        var emails = new[] { timesheet.PersonEmail, worker?.ContactEmail ?? "" }.Where(email => email.Length > 0).ToList();
        var notes = await context.ProgressUpdates.AsNoTracking()
            .Where(row => row.ProjectId == timesheet.ProjectId && row.WorkDate == timesheet.WorkedOn && emails.Contains(row.CreatedByEmail))
            .OrderByDescending(row => row.CreatedAt)
            .ToListAsync(cancellationToken);
        return notes.FirstOrDefault(row => !MyDayNotes.IsOffDay(row.Title));
    }

    private async Task<IReadOnlyList<SubmittedDayPhoto>> PhotosOfAsync(ProgressUpdateEntity? note, CancellationToken cancellationToken)
    {
        if (note is null) return Array.Empty<SubmittedDayPhoto>();
        return await context.ProgressPhotos.AsNoTracking()
            .Where(photo => photo.ProgressUpdateId == note.ProgressUpdateId)
            .OrderBy(photo => photo.SortOrder)
            .Select(photo => new SubmittedDayPhoto(photo.ProgressPhotoId, photo.FileName))
            .ToListAsync(cancellationToken);
    }

    private async Task<MyDayRaisedReferences.References> RaisedByAsync(ProgressUpdateEntity? note, CancellationToken cancellationToken)
    {
        if (note is null) return new MyDayRaisedReferences.References();
        var references = await MyDayRaisedReferences.ForNotesAsync(context, new[] { note.ProgressUpdateId }, cancellationToken);
        return references.For(note.ProgressUpdateId);
    }
}
