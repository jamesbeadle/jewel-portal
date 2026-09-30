using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>One record per site per day: before a day is logged, filled in late or recorded off,
/// the day must hold nothing for that worker on that site — no sign-in at the gate, no timesheet,
/// no note. A day already there is amended from the week, never written twice.</summary>
public static class MyDayVacancy
{
    public static async Task EnsureAsync(
        JpmsContext context, string projectId, WorkerEntity worker, string email, DateTimeOffset workDate, CancellationToken cancellationToken)
    {
        var isSignedIn = await context.SiteAttendances.AnyAsync(
            row => row.ProjectId == projectId && row.WorkerId == worker.WorkerId && row.WorkDate == workDate, cancellationToken);
        if (isSignedIn) throw new InvalidOperationException("You're signed in on that day — sign out to log it instead.");
        var hasTimesheet = await context.Timesheets.AnyAsync(
            row => row.ProjectId == projectId && row.WorkerId == worker.WorkerId && row.WorkedOn == workDate, cancellationToken);
        var hasNote = await context.ProgressUpdates.AnyAsync(
            row => row.ProjectId == projectId && row.CreatedByEmail == email && row.WorkDate == workDate, cancellationToken);
        if (hasTimesheet || hasNote) throw new InvalidOperationException("That day is already on this site — amend it from the week instead.");
    }
}
