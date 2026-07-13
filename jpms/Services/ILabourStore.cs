using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

/// <summary>
/// Front-end store for labour tracking (docs/Labour-Time-Tracking-Scope.md). Synchronous reads
/// follow the fetch-once-per-key convention (CLAUDE.md): pages call the Refresh methods once
/// from OnInitializedAsync so tab navigation revalidates in the background.
/// </summary>
public interface ILabourStore
{
    event Action? OnChange;

    // Registry (commercial team only — includes rates).
    IReadOnlyList<Worker> Workers();
    Task RefreshWorkersAsync();
    Task<Worker> AddWorkerAsync(string name, decimal hourlyRate, string? subcontractorId, string contactEmail, string contactPhone);
    Task<Worker> UpdateWorkerAsync(Worker worker);

    // Project assignment.
    IReadOnlyList<ProjectWorkerAssignment> AssignmentsFor(string projectId);
    Task RefreshAssignmentsAsync(string projectId);
    Task SetAssignmentAsync(string projectId, string workerId, bool isActive);

    // Site access (QR).
    SiteAccess? SiteAccessFor(string projectId);
    Task RefreshSiteAccessAsync(string projectId);
    Task RotateSiteAccessAsync(string projectId);

    // Timesheets + register.
    IReadOnlyList<TimesheetDetail> TimesheetsFor(string projectId);
    Task RefreshTimesheetsAsync(string projectId);
    IReadOnlyList<SiteAttendance> AttendanceFor(string projectId);
    Task RefreshAttendanceAsync(string projectId);

    Task<TimesheetDetail> AddWorkerTimesheetAsync(string projectId, string workerId, DateTimeOffset workedOn, decimal hours, string costCode);
    Task<TimesheetDetail> AdjustTimesheetAsync(string projectId, string timesheetId, decimal hours, string costCode);
    Task<LabourApprovalResult> ApproveTimesheetsAsync(string projectId, IReadOnlyList<string> timesheetIds);
    Task<TimesheetDetail> RejectTimesheetAsync(string projectId, string timesheetId, string reason);

    // Settlement reconciliation.
    IReadOnlyList<LabourSettlementRow> SettlementFor(string projectId);
    Task RefreshSettlementAsync(string projectId);
    Task SetTimesheetCoverAsync(string projectId, string xeroLedgerLineId, bool isCovered, string subcontractorId, DateTimeOffset periodStart, DateTimeOffset periodEnd);
    Task AddSettlementVarianceAsync(string projectId, string costCode, string subcontractorId, decimal amount, string reason, string? xeroLedgerLineId);
}
