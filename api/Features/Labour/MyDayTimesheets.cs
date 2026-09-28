using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>The timesheet a worker's own sign-out writes: one Submitted row per cost code, tied to
/// the day's attendance, waiting for the Project Manager's approval before it is cost.</summary>
public static class MyDayTimesheets
{
    public static TimesheetEntity Submitted(
        string projectId, SiteSignOutEntry entry, WorkerEntity worker, SiteAttendanceEntity attendance, string email, DateTimeOffset today) =>
        new()
        {
            TimesheetId = CommercialIdentifierFactory.NextTimesheetId(),
            ProjectId = projectId,
            PersonEmail = email,
            WorkerId = worker.WorkerId,
            SiteAttendanceId = attendance.SiteAttendanceId,
            WorkedOn = today,
            Hours = entry.Hours,
            CostCode = entry.CostCode,
            Status = (int)TimesheetStatus.Submitted,
            IsApproved = false
        };
}
