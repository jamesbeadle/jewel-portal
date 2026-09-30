using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>The timesheet a worker's own day writes: one Submitted row per cost code, waiting for
/// the Project Manager's approval before it is cost. A sign-out ties it to the day's attendance;
/// a missed day filled in later has no attendance and is marked as filed late.</summary>
public static class MyDayTimesheets
{
    public static TimesheetEntity Submitted(
        string projectId, SiteSignOutEntry entry, WorkerEntity worker, SiteAttendanceEntity attendance, string email, DateTimeOffset today) =>
        Row(projectId, entry, worker, email, today, attendance.SiteAttendanceId, isFiledLate: false);

    public static TimesheetEntity FiledLate(
        string projectId, SiteSignOutEntry entry, WorkerEntity worker, string email, DateTimeOffset workDate) =>
        Row(projectId, entry, worker, email, workDate, siteAttendanceId: "", isFiledLate: true);

    private static TimesheetEntity Row(
        string projectId, SiteSignOutEntry entry, WorkerEntity worker, string email, DateTimeOffset workDate, string siteAttendanceId, bool isFiledLate) =>
        new()
        {
            TimesheetId = CommercialIdentifierFactory.NextTimesheetId(),
            ProjectId = projectId,
            PersonEmail = email,
            WorkerId = worker.WorkerId,
            SiteAttendanceId = siteAttendanceId,
            WorkedOn = workDate,
            Hours = entry.Hours,
            CostCode = entry.CostCode,
            Status = (int)TimesheetStatus.Submitted,
            IsApproved = false,
            IsFiledLate = isFiledLate
        };
}
