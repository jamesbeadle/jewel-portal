namespace Jewel.JPMS.Models;

/// <summary>
/// Lifecycle of a timesheet under labour tracking (docs/Labour-Time-Tracking-Scope.md).
/// Submitted time is visible as pending labour; only Approved time becomes actual cost of
/// sales. Rejected timesheets re-open for the worker to resubmit (no deadline enforced).
/// </summary>
public enum TimesheetStatus
{
    Submitted = 0,
    Approved = 1,
    Rejected = 2,
}

/// <summary>
/// A site operative (day-rate subcontractor labour). HourlyRate is the agreed day rate ÷ 8;
/// it is only ever returned to commercial roles and never to the site capture page.
/// </summary>
public sealed record Worker(
    string WorkerId,
    string Name,
    string? SubcontractorId,
    decimal HourlyRate,
    bool IsActive,
    string ContactEmail,
    string ContactPhone);

public sealed record ProjectWorkerAssignment(
    string ProjectWorkerAssignmentId,
    string ProjectId,
    string WorkerId,
    string WorkerName,
    bool IsActive);

/// <summary>One worker-day on site — the daily site register row.</summary>
public sealed record SiteAttendance(
    string SiteAttendanceId,
    string ProjectId,
    string WorkerId,
    string WorkerName,
    DateTimeOffset WorkDate,
    DateTimeOffset SignedInAt,
    DateTimeOffset? SignedOutAt);

/// <summary>
/// A timesheet with labour-tracking detail for the PM's Labour tab. RateApplied/CostAmount are
/// zero until approval snapshots them, and are zeroed in responses to non-commercial roles.
/// </summary>
public sealed record TimesheetDetail(
    string TimesheetId,
    string ProjectId,
    string WorkerId,
    string WorkerName,
    DateTimeOffset WorkedOn,
    decimal Hours,
    string CostCode,
    TimesheetStatus Status,
    decimal RateApplied,
    decimal CostAmount,
    string ApprovedByEmail,
    DateTimeOffset? ApprovedAt,
    string RejectionReason);

public sealed record LabourApprovalFailure(string TimesheetId, string Reason);

/// <summary>
/// Outcome of a batch approval. Failures carry the budget hard-block or validation reason per
/// timesheet — approve-what-you-can, report the rest (workflow 07-D).
/// </summary>
public sealed record LabourApprovalResult(
    IReadOnlyList<TimesheetDetail> Approved,
    IReadOnlyList<LabourApprovalFailure> Failures);

/// <summary>
/// Invoiced vs approved per subcontractor for the settlement reconciliation view: approved
/// timesheet £, Xero lines marked covered-by-timesheets, posted settlement variances, and the
/// residual variance still to resolve.
/// </summary>
public sealed record LabourSettlementRow(
    string SubcontractorId,
    string SubcontractorName,
    decimal ApprovedCost,
    decimal CoveredInvoiceTotal,
    decimal PostedVarianceTotal,
    decimal UnresolvedVariance);

public sealed record LabourSettlementVariance(
    string LabourSettlementVarianceId,
    string ProjectId,
    string CostCode,
    string SubcontractorId,
    decimal Amount,
    string Reason,
    string? XeroLedgerLineId,
    string CreatedByEmail,
    DateTimeOffset CreatedAt);

// --- "My day" DTOs: the worker-facing shapes on the authenticated self-service page. ---
// Workers are normal portal users (SiteOperative role); these shapes still carry no rates
// or £ — hours only.

public sealed record SiteSheetCostCode(string Code, string Name);

public sealed record SiteSignOutEntry(string CostCode, decimal Hours);

/// <summary>Today's state for the signed-in worker across their assigned projects.</summary>
public sealed record MyLabourDay(
    string WorkerId,
    string WorkerName,
    DateTimeOffset WorkDate,
    IReadOnlyList<MyLabourProject> Projects,
    IReadOnlyList<MyRejectedTimesheet> Rejected);

public sealed record MyLabourProject(
    string ProjectId,
    string ProjectName,
    bool IsSignedInToday,
    bool HasSignedOutToday,
    IReadOnlyList<SiteSheetCostCode> CostCodes);

/// <summary>One of the caller's rejected timesheets — re-opened for correction. Hours only.</summary>
public sealed record MyRejectedTimesheet(
    string TimesheetId,
    string ProjectId,
    string ProjectName,
    DateTimeOffset WorkedOn,
    decimal Hours,
    string CostCode,
    string RejectionReason);
