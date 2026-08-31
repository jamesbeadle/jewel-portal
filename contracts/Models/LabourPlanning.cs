namespace Jewel.JPMS.Models;

/// <summary>Kinds of recorded absence. Holiday, half day and not-worked mirror the JPS
/// forecast's deduction categories; Sick is kept distinct for reporting.</summary>
public enum AbsenceKind
{
    Holiday = 0,
    HalfDay = 1,
    NotWorked = 2,
    Sick = 3,
}

/// <summary>Nature of a settlement line — decides the Xero account and the CIS treatment
/// (only CisLabour is deducted).</summary>
public enum SettlementLineNature
{
    CisLabour = 0,
    CisMaterials = 1,
    Travel = 2,
}

/// <summary>Why a worker-day is on the chase list.</summary>
public enum LabourChaseReason
{
    NoTimesheet = 0,
    OpenAttendance = 1,
}

public sealed record WorkerAbsence(
    string WorkerAbsenceId,
    string WorkerId,
    string WorkerName,
    DateTimeOffset Date,
    AbsenceKind Kind,
    string Note,
    string RecordedByEmail,
    DateTimeOffset RecordedAt);

/// <summary>One cell of the placement grid: what one worker did on one date. Absence and
/// timesheet are mutually exclusive in practice; both empty = nothing recorded.</summary>
public sealed record LabourOverviewDay(
    DateTimeOffset Date,
    string ProjectId,
    string ProjectName,
    decimal Hours,
    TimesheetStatus? Status,
    AbsenceKind? Absence);

/// <summary>A signed-off week for a worker (WeekStart is the Monday).</summary>
public sealed record LabourWeekSignOff(
    string WorkerId,
    DateTimeOffset WeekStart,
    string SignedOffByEmail,
    DateTimeOffset SignedOffAt);

/// <summary>
/// One worker's month on the Labour overview: forecast inputs, recorded days, projected cost and
/// the net payable after CIS. Money fields are only ever returned to commercial roles — the
/// endpoint is gated, not stripped.
/// </summary>
public sealed record LabourOverviewWorker(
    string WorkerId,
    string Name,
    decimal DayRate,
    decimal ContractedDays,
    decimal CisRatePercent,
    decimal DaysWorked,
    decimal DaysOff,
    decimal ProjectedCost,
    decimal AmountDue,
    IReadOnlyList<LabourOverviewDay> Days,
    IReadOnlyList<LabourWeekSignOff> SignOffs);

/// <summary>One site's (project's) recorded labour for the month.</summary>
public sealed record LabourOverviewSite(
    string ProjectId,
    string ProjectName,
    decimal DaysRecorded,
    decimal CostRecorded);

/// <summary>One cost code's recorded labour for the month, with the trade grouping when one is
/// configured (empty string when not).</summary>
public sealed record LabourOverviewCostCode(
    string CostCode,
    string Trade,
    decimal DaysRecorded,
    decimal CostRecorded);

public sealed record LabourChaseItem(
    string WorkerId,
    string WorkerName,
    DateTimeOffset Date,
    LabourChaseReason Reason,
    string ProjectId,
    string ProjectName);

/// <summary>One week's segment of the submission-confidence bar.</summary>
public sealed record LabourWeekConfidence(
    DateTimeOffset WeekStart,
    int ElapsedWorkerDays,
    int ConfirmedWorkerDays,
    decimal UnconfirmedCost);

/// <summary>Header figures for the Labour overview month.</summary>
public sealed record LabourOverviewTotals(
    decimal ProjectedSpend,
    decimal TimeOffCost,
    decimal AmountDueTotal,
    int ElapsedWorkerDays,
    int ConfirmedWorkerDays,
    decimal UnconfirmedCost,
    IReadOnlyList<LabourWeekConfidence> Weeks);

/// <summary>The whole Labour overview for one month — the single read behind /labour/overview.</summary>
public sealed record LabourOverviewSnapshot(
    int Year,
    int Month,
    LabourOverviewTotals Totals,
    IReadOnlyList<LabourOverviewWorker> Workers,
    IReadOnlyList<LabourOverviewSite> Sites,
    IReadOnlyList<LabourOverviewCostCode> CostCodes,
    IReadOnlyList<LabourChaseItem> Chase,
    // Dismissed chase-days that would otherwise appear this month (2026-08-31) — kept visible as
    // a count so a clean list is legible as "reviewed", never "unchecked".
    int DismissedThisMonth = 0);
