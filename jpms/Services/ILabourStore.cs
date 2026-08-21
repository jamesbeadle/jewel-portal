using Jewel.JPMS.Contracts.Labour;
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
    /// <summary>False until the registry has landed — the synchronous reads answer with empty
    /// lists in the meantime, which no view can tell apart from a real empty result.</summary>
    bool WorkersLoaded { get; }
    Task RefreshWorkersAsync();
    Task<Worker> AddWorkerAsync(string name, decimal hourlyRate, string? subcontractorId, string contactEmail, string contactPhone);
    Task<Worker> UpdateWorkerAsync(Worker worker);
    Task DeleteWorkerAsync(string workerId);

    // Project assignment.
    IReadOnlyList<ProjectWorkerAssignment> AssignmentsFor(string projectId);
    /// <summary>False until this project's assignments have landed — see <see cref="WorkersLoaded"/>.</summary>
    bool AssignmentsLoadedFor(string projectId);
    Task RefreshAssignmentsAsync(string projectId);
    Task SetAssignmentAsync(string projectId, string workerId, bool isActive);

    // My Day — the signed-in worker's own timesheet surface.
    MyLabourDay? MyDay();
    Task RefreshMyDayAsync();
    Task MySignInAsync(string projectId);
    Task MySignOutAsync(string projectId, IReadOnlyList<SiteSignOutEntry> entries);
    Task MyResubmitAsync(string timesheetId, decimal hours, string costCode);

    // Timesheets + register.
    IReadOnlyList<TimesheetDetail> TimesheetsFor(string projectId);
    /// <summary>False until this project's timesheets have landed — see <see cref="WorkersLoaded"/>.</summary>
    bool TimesheetsLoadedFor(string projectId);
    Task RefreshTimesheetsAsync(string projectId);
    IReadOnlyList<SiteAttendance> AttendanceFor(string projectId);
    /// <summary>False until this project's register has landed — see <see cref="WorkersLoaded"/>.</summary>
    bool AttendanceLoadedFor(string projectId);
    Task RefreshAttendanceAsync(string projectId);

    Task<TimesheetDetail> AddWorkerTimesheetAsync(string projectId, string workerId, DateTimeOffset workedOn, decimal hours, string costCode);
    /// <summary>The accountant's weekly entry: one worker's week of site days in one command.
    /// Refreshes the overview for every month the days touch (a week can straddle two).</summary>
    Task<WorkerWeekResult> SubmitWorkerWeekAsync(string workerId, DateTimeOffset weekStart, IReadOnlyList<WorkerWeekDayEntry> days);
    Task<TimesheetDetail> AdjustTimesheetAsync(string projectId, string timesheetId, decimal hours, string costCode);
    Task<LabourApprovalResult> ApproveTimesheetsAsync(string projectId, IReadOnlyList<string> timesheetIds);
    Task<TimesheetDetail> RejectTimesheetAsync(string projectId, string timesheetId, string reason);

    // Labour overview: the company-wide month view (forecast, placement grid, chase, sign-off).
    LabourOverviewSnapshot? Overview(int year, int month);
    /// <summary>False until this month's overview has landed — see <see cref="WorkersLoaded"/>.</summary>
    bool OverviewLoadedFor(int year, int month);
    Task RefreshOverviewAsync(int year, int month);
    Task SetWorkerContractAsync(int year, int month, string workerId, decimal contractedDaysPerMonth);
    Task SetWorkerCisStatusAsync(int year, int month, string workerId, decimal cisRatePercent, string verifiedRef);
    Task RecordAbsenceAsync(int year, int month, string workerId, DateTimeOffset date, AbsenceKind kind, string note);
    Task RemoveAbsenceAsync(int year, int month, string workerAbsenceId);
    Task SignOffWeekAsync(int year, int month, string workerId, DateTimeOffset weekStart);
    Task RemoveWeekSignOffAsync(int year, int month, string workerId, DateTimeOffset weekStart);

    // Settlement schedules (per worker-month), Xero mappings, and the coding run.
    SettlementScheduleSnapshot? Schedules(int year, int month);
    /// <summary>False until this month's schedules have landed — see <see cref="WorkersLoaded"/>.</summary>
    bool SchedulesLoadedFor(int year, int month);
    Task RefreshSchedulesAsync(int year, int month);
    Task AddSettlementLineAsync(int year, int month, string workerId, string projectId, string costCode, SettlementLineNature nature, decimal amount, string note);
    Task RemoveSettlementLineAsync(int year, int month, string workerSettlementLineId);
    XeroMappingsSnapshot? XeroMappings();
    Task RefreshXeroMappingsAsync();
    Task SetSiteXeroMappingAsync(string projectId, string optionId, string optionName);
    Task SetCostCodeXeroMappingAsync(string costCode, string optionId, string optionName, string labourAccount, string materialsAccount, string travelAccount);
    Task<IReadOnlyList<XeroCodingRunResult>> RunXeroCodingAsync(int year, int month, IReadOnlyList<string>? workerIds);

    // Settlement reconciliation.
    IReadOnlyList<LabourSettlementRow> SettlementFor(string projectId);
    /// <summary>False until this project's settlement rows have landed — see <see cref="WorkersLoaded"/>.</summary>
    bool SettlementLoadedFor(string projectId);
    Task RefreshSettlementAsync(string projectId);
    Task SetTimesheetCoverAsync(string projectId, string xeroLedgerLineId, bool isCovered, string subcontractorId, DateTimeOffset periodStart, DateTimeOffset periodEnd);
    Task AddSettlementVarianceAsync(string projectId, string costCode, string subcontractorId, decimal amount, string reason, string? xeroLedgerLineId);
}
