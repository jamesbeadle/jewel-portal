using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// A worker's contracted days per month, effective-dated like rate history
/// (docs/Labour-Overview-Forecast-and-Xero-Mapping-Scope.md §3). Drives the projected labour
/// spend baseline: the forecast for a month uses the contract effective in that month.
/// </summary>
public sealed class WorkerContractEntity
{
    [Key, MaxLength(64)] public string WorkerContractId { get; set; } = "";
    [MaxLength(64)]      public string WorkerId { get; set; } = "";
    public decimal ContractedDaysPerMonth { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
}

/// <summary>
/// One worker's absence on one date. Absence reduces the forecast at the day rate (half for a
/// half day) and explains a missing timesheet day on the sign-off grid so it is not chased.
/// Kind mirrors the contracts AbsenceKind enum.
/// </summary>
public sealed class WorkerAbsenceEntity
{
    [Key, MaxLength(64)] public string WorkerAbsenceId { get; set; } = "";
    [MaxLength(64)]      public string WorkerId { get; set; } = "";
    // The date (midnight UTC), same convention as SiteAttendanceEntity.WorkDate.
    public DateTimeOffset Date { get; set; }
    public int Kind { get; set; }
    [MaxLength(512)]     public string Note { get; set; } = "";
    [MaxLength(256)]     public string RecordedByEmail { get; set; } = "";
    public DateTimeOffset RecordedAt { get; set; }
}

/// <summary>
/// A worker's CIS deduction status, effective-dated. Standard deduction is 20%; unverified is
/// 30%; gross status is 0%. Used only for the net-payable line on the forecast and settlement
/// schedule — JPMS does not run CIS verification or returns (that stays in Xero/HMRC tooling).
/// </summary>
public sealed class WorkerCisStatusEntity
{
    [Key, MaxLength(64)] public string WorkerCisStatusId { get; set; } = "";
    [MaxLength(64)]      public string WorkerId { get; set; } = "";
    public decimal CisRatePercent { get; set; }
    [MaxLength(64)]      public string VerifiedRef { get; set; } = "";
    public DateTimeOffset EffectiveFrom { get; set; }
}

/// <summary>
/// The per-worker weekly sign-off marker (scope §4): records that a PM signed this worker's week
/// off after every elapsed day was approved, rejected-and-explained, or covered by an absence.
/// Sign-off is a view over approval, not a second state machine — deleting the row un-signs the
/// week without touching any timesheet.
/// </summary>
public sealed class LabourWeekSignOffEntity
{
    [Key, MaxLength(64)] public string LabourWeekSignOffId { get; set; } = "";
    [MaxLength(64)]      public string WorkerId { get; set; } = "";
    // Monday of the week (midnight UTC).
    public DateTimeOffset WeekStart { get; set; }
    [MaxLength(256)]     public string SignedOffByEmail { get; set; } = "";
    public DateTimeOffset SignedOffAt { get; set; }
}

/// <summary>
/// A non-labour settlement line added at sign-off level for a worker whose arrangement includes
/// materials or travel (scope §6): these change both the Xero account and the CIS treatment
/// (no CIS deduction). Month is the first of the month (midnight UTC).
/// </summary>
public sealed class WorkerSettlementLineEntity
{
    [Key, MaxLength(64)] public string WorkerSettlementLineId { get; set; } = "";
    [MaxLength(64)]      public string WorkerId { get; set; } = "";
    public DateTimeOffset Month { get; set; }
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(32)]      public string CostCode { get; set; } = "";
    public int Nature { get; set; }
    public decimal Amount { get; set; }
    [MaxLength(512)]     public string Note { get; set; } = "";
    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Effective-dated bridge from a JPMS project to the Xero site tracking option (scope §6):
/// Xero options are never renamed to chase portal names; reports and the coding automation
/// translate through this map, so historic and current data both read correctly.
/// </summary>
public sealed class SiteXeroMappingEntity
{
    [Key, MaxLength(64)] public string SiteXeroMappingId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string XeroTrackingOptionId { get; set; } = "";
    [MaxLength(256)]     public string XeroTrackingOptionName { get; set; } = "";
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
}

/// <summary>
/// Effective-dated bridge from a portal cost code to the Xero cost-code tracking option and the
/// account code used per line nature. AccountCode fields empty = fall back to the coding run's
/// configured defaults per nature.
/// </summary>
public sealed class CostCodeXeroMappingEntity
{
    [Key, MaxLength(64)] public string CostCodeXeroMappingId { get; set; } = "";
    [MaxLength(32)]      public string CostCode { get; set; } = "";
    [MaxLength(64)]      public string XeroTrackingOptionId { get; set; } = "";
    [MaxLength(256)]     public string XeroTrackingOptionName { get; set; } = "";
    [MaxLength(32)]      public string LabourAccountCode { get; set; } = "";
    [MaxLength(32)]      public string MaterialsAccountCode { get; set; } = "";
    [MaxLength(32)]      public string TravelAccountCode { get; set; } = "";
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
}

/// <summary>
/// One automated Xero coding run for one worker-month (scope §6a). Every write the automation
/// makes is recorded here; gaps are recorded as skipped lines so nothing is silently guessed.
/// </summary>
public sealed class XeroCodingRunEntity
{
    [Key, MaxLength(64)] public string XeroCodingRunId { get; set; } = "";
    [MaxLength(64)]      public string WorkerId { get; set; } = "";
    public DateTimeOffset Month { get; set; }
    public int Outcome { get; set; }
    [MaxLength(140)]     public string XeroBillId { get; set; } = "";
    [MaxLength(2048)]    public string Detail { get; set; } = "";
    [MaxLength(256)]     public string RunByEmail { get; set; } = "";
    public DateTimeOffset RunAt { get; set; }
}

/// <summary>
/// A dismissed chase-list day (2026-08-31): the FD or PM has looked at "no timesheet and no
/// recorded absence" for this worker-day and decided it needs no chasing — with a reason, so the
/// decision is auditable. The chase generator excludes dismissed days from the list AND from the
/// unconfirmed-cost accrual; a timesheet or absence recorded later simply supersedes the row
/// (the day is then confirmed and the dismissal is moot). One row per worker per day.
/// </summary>
public sealed class LabourChaseDismissalEntity
{
    [Key, MaxLength(64)] public string LabourChaseDismissalId { get; set; } = "";
    [MaxLength(64)]      public string WorkerId { get; set; } = "";
    // The date (midnight UTC), same convention as WorkerAbsenceEntity.Date.
    public DateTimeOffset Date { get; set; }
    [MaxLength(512)]     public string Reason { get; set; } = "";
    [MaxLength(256)]     public string DismissedByEmail { get; set; } = "";
    public DateTimeOffset DismissedAt { get; set; }
}
