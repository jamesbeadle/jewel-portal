namespace Jewel.JPMS.Models;

/// <summary>The per-worker monthly reconciliation verdict (scope §6).</summary>
public enum ScheduleVerdict
{
    /// <summary>Covered bill total equals the schedule gross (to the penny).</summary>
    Matches = 0,
    /// <summary>A bill is covered but its total differs from the schedule.</summary>
    VarianceOpen = 1,
    /// <summary>No covering bill has been marked for the period yet — chase it.</summary>
    NoBillYet = 2,
    /// <summary>Nothing approved and nothing invoiced — an empty month.</summary>
    Nothing = 3,
}

/// <summary>One line of a worker's settlement schedule: site × cost code × nature.</summary>
public sealed record ScheduleLine(
    string ProjectId,
    string ProjectName,
    string CostCode,
    SettlementLineNature Nature,
    decimal Amount,
    /// <summary>Set on manually added (materials/travel) lines so they can be removed.</summary>
    string? WorkerSettlementLineId);

/// <summary>
/// One worker's settlement schedule for one month: what the signed-off timesheets say they are
/// owed, split the way the covering bill should be coded in Dext/Xero, with the CIS deduction
/// and the reconciliation verdict against what Xero actually holds.
/// </summary>
public sealed record WorkerSettlementSchedule(
    string WorkerId,
    string WorkerName,
    string? SubcontractorId,
    string SubcontractorName,
    IReadOnlyList<ScheduleLine> Lines,
    decimal GrossLabour,
    decimal GrossOther,
    decimal GrossTotal,
    decimal CisRatePercent,
    decimal CisDeduction,
    decimal NetPayable,
    decimal CoveredBillTotal,
    decimal Difference,
    ScheduleVerdict Verdict,
    /// <summary>True when every week with approved time in the month carries a sign-off marker —
    /// the §6a automation refuses a worker-month that is not fully signed off.</summary>
    bool FullySignedOff,
    /// <summary>The latest Xero coding run for this worker-month, empty when none has run.</summary>
    string LastCodingOutcome,
    DateTimeOffset? LastCodedAt);

/// <summary>The month's schedules plus the chase counts the dashboard chips show.</summary>
public sealed record SettlementScheduleSnapshot(
    int Year,
    int Month,
    IReadOnlyList<WorkerSettlementSchedule> Workers,
    int InvoicesToChase,
    int WorkersToReconcile);

// ---- Xero mapping admin (scope §3/§6) -------------------------------------------------------

public sealed record SiteXeroMapping(
    string SiteXeroMappingId,
    string ProjectId,
    string ProjectName,
    string XeroTrackingOptionId,
    string XeroTrackingOptionName,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo);

public sealed record CostCodeXeroMapping(
    string CostCodeXeroMappingId,
    string CostCode,
    string XeroTrackingOptionId,
    string XeroTrackingOptionName,
    string LabourAccountCode,
    string MaterialsAccountCode,
    string TravelAccountCode,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo);

public sealed record XeroMappingsSnapshot(
    IReadOnlyList<SiteXeroMapping> Sites,
    IReadOnlyList<CostCodeXeroMapping> CostCodes);

// ---- §6a: the automated coding run ----------------------------------------------------------

public enum XeroCodingOutcome
{
    /// <summary>A Dext-arrived draft bill was recoded to the schedule.</summary>
    BillRecoded = 0,
    /// <summary>No bill had arrived; a draft bill matching the schedule was staged.</summary>
    DraftStaged = 1,
    /// <summary>The run skipped this worker-month and reported why (mapping gap, not signed
    /// off, already coded…). Nothing was written.</summary>
    Skipped = 2,
    /// <summary>Xero rejected the write; Detail carries its own words.</summary>
    Failed = 3,
}

public sealed record XeroCodingRunResult(
    string WorkerId,
    string WorkerName,
    XeroCodingOutcome Outcome,
    string Detail,
    string XeroBillId);
