using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>A worker's day put on another of their sites — at sign-out, when the work was not where
/// they signed in, or by an amendment afterwards. The site must be on their list and have no day of
/// theirs on that date; the attendance, the hours and the note move together, never one without
/// the others.</summary>
public static class MyDaySiteMove
{
    public static async Task<string> DestinationAsync(
        JpmsContext context, WorkerEntity worker, string currentProjectId, string wantedProjectId, DateTimeOffset workDate, CancellationToken cancellationToken)
    {
        var isStayingPut = string.IsNullOrWhiteSpace(wantedProjectId) || wantedProjectId == currentProjectId;
        if (isStayingPut) return currentProjectId;
        await MyDayAssignment.EnsureAssignedAsync(context, wantedProjectId, worker, cancellationToken);
        var isThatSiteTaken = await context.Timesheets.AnyAsync(
            row => row.WorkerId == worker.WorkerId && row.ProjectId == wantedProjectId && row.WorkedOn == workDate, cancellationToken);
        if (isThatSiteTaken) throw new InvalidOperationException("That site already has a day logged on this date — amend that one instead.");
        return wantedProjectId;
    }

    public static void Move(TimesheetEntity timesheet, ProgressUpdateEntity note, SiteAttendanceEntity? attendance, string projectId)
    {
        timesheet.ProjectId = projectId;
        note.ProjectId = projectId;
        if (attendance is not null) attendance.ProjectId = projectId;
    }
}
