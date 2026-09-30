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
// IsSoleTrader / engagement window (2026-08-31): a sole trader bills under their own name and
// is their own settlement counterparty (ignored when SubcontractorId is set — a company link
// wins); EngagedFrom/To bound what the chase list EXPECTS, never what counts. Trailing optionals
// so jpms's positional constructions stay valid.
public sealed record Worker(
    string WorkerId,
    string Name,
    string? SubcontractorId,
    decimal HourlyRate,
    bool IsActive,
    string ContactEmail,
    string ContactPhone,
    bool IsSoleTrader = false,
    DateTimeOffset? EngagedFrom = null,
    DateTimeOffset? EngagedTo = null,
    // Stamped by RetireWorker: contact details cleared, engagement closed, name and timesheets kept.
    DateTimeOffset? RetiredAt = null)
{
    public bool HasBeenRetired => RetiredAt is not null;
}

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
    string RejectionReason,
    bool IsFiledLate = false);

/// <summary>BudgetBlocked marks the failures the budget hard-block produced (as opposed to
/// uncoded rows, missing workers, already-decided rows). The Labour tab uses it to offer the
/// MD/FD-only "Approve over budget" follow-up on exactly those rows and no others.</summary>
public sealed record LabourApprovalFailure(string TimesheetId, string Reason, bool BudgetBlocked = false);

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

/// <summary>Today's state for the signed-in worker across their assigned projects, plus their
/// recent timesheets so they can see what they submitted and whether it was approved. A login
/// with no linked, active Worker record is answered with an unlinked day: no worker, nothing to log.</summary>
public sealed record MyLabourDay(
    string WorkerId,
    string WorkerName,
    DateTimeOffset WorkDate,
    IReadOnlyList<MyLabourProject> Projects,
    IReadOnlyList<MyRejectedTimesheet> Rejected,
    IReadOnlyList<MyRecentTimesheet> Recent,
    IReadOnlyList<MyWeekDay> Week)
{
    public bool IsLinked => !string.IsNullOrEmpty(WorkerId);
}

/// <summary>What one site holds for one day of the worker's week: a logged day, a recorded day
/// off, or nothing at all.</summary>
public enum MyWeekDayKind { Logged, Off, Nothing }

/// <summary>One site on one day of the worker's week, Monday to today, as My day lists it so the
/// worker sees what they have and have not filed before Friday. A logged day carries what its
/// amend form pre-fills and the note its photographs go onto; it may be amended until the office
/// has approved it. A day with nothing on it, once gone, may be filled in late.</summary>
public sealed record MyWeekDay(
    string ProjectId,
    string ProjectName,
    DateTimeOffset Date,
    MyWeekDayKind Kind,
    string TimesheetId = "",
    decimal Hours = 0m,
    string CostCode = "",
    TimesheetStatus? Status = null,
    string Words = "",
    int PhotoCount = 0,
    DateTimeOffset? SignedOutAt = null,
    string ProgressUpdateId = "",
    bool IsFiledLate = false)
{
    public bool IsLogged => Kind == MyWeekDayKind.Logged;
    public bool CanBeAmended => IsLogged && Status is not TimesheetStatus.Approved;
    public bool IsMissed => Kind == MyWeekDayKind.Nothing;
    public bool HasNote => ProgressUpdateId.Length > 0;
}

/// <summary>One of the caller's recent timesheets (last two weeks) — hours and status only.</summary>
public sealed record MyRecentTimesheet(
    string TimesheetId,
    string ProjectId,
    string ProjectName,
    DateTimeOffset WorkedOn,
    decimal Hours,
    string CostCode,
    TimesheetStatus Status);

public sealed record MyLabourProject(
    string ProjectId,
    string ProjectName,
    bool IsSignedInToday,
    bool HasSignedOutToday,
    IReadOnlyList<SiteSheetCostCode> CostCodes,
    DateTimeOffset? SignedInAt = null,
    DateTimeOffset? SignedOutAt = null,
    MyDayNote? TodaysNote = null)
{
    public bool IsLoggedToday => HasSignedOutToday || TodaysNote is not null;
}

/// <summary>The day's note as the worker logged it — the progress update in their own name that
/// the Contractor's Report reads — with how many photographs it holds so far, and the references
/// of the records the log raised beside it (a Site Instruction, a defect), empty when none.</summary>
public sealed record MyDayNote(
    string ProgressUpdateId,
    string Title,
    string Description,
    int PhotoCount,
    string SiteInstructionReference = "",
    string DefectReference = "");

/// <summary>What logging a day answers: the attendance closed (empty on an off day), the note the
/// photographs go onto, and the references of the records raised beside it, empty when none.</summary>
public sealed record MySiteDayLogged(
    string SiteAttendanceId,
    string ProgressUpdateId,
    string SiteInstructionReference = "",
    string DefectReference = "");

/// <summary>One of the caller's rejected timesheets — re-opened for correction. Hours only.</summary>
public sealed record MyRejectedTimesheet(
    string TimesheetId,
    string ProjectId,
    string ProjectName,
    DateTimeOffset WorkedOn,
    decimal Hours,
    string CostCode,
    string RejectionReason);
