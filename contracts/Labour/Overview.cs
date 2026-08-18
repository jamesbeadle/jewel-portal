using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

// The company-wide Labour overview (scope §4): by worker / by site / by cost code for one month,
// the forecast header, the chase list and weekly sign-off. Money rides on every shape here, so
// the endpoint is gated to the commercial roles — nothing in this file reaches a worker-facing
// surface.

/// <summary>The whole overview for one month in a single read.</summary>
public sealed record GetLabourOverview(int Year, int Month) : IQuery<LabourOverviewSnapshot>;

/// <summary>Sets a worker's contracted days per month (effective now) — the forecast baseline.
/// Appends to the effective-dated history; past months keep the contract effective then.</summary>
public sealed record SetWorkerContract(string WorkerId, decimal ContractedDaysPerMonth)
    : ICommand<Acknowledgement>;

/// <summary>Sets a worker's CIS deduction status (effective now): 20 standard, 30 unverified,
/// 0 gross. VerifiedRef is the HMRC verification reference where one exists.</summary>
public sealed record SetWorkerCisStatus(string WorkerId, decimal CisRatePercent, string VerifiedRef)
    : ICommand<Acknowledgement>;

/// <summary>Records one worker's absence on one date. One absence per worker per date —
/// recording again replaces the kind/note.</summary>
public sealed record RecordWorkerAbsence(string WorkerId, DateTimeOffset Date, AbsenceKind Kind, string Note)
    : ICommand<WorkerAbsence>;

public sealed record RemoveWorkerAbsence(string WorkerAbsenceId) : ICommand<Acknowledgement>;

/// <summary>
/// Signs off one worker's week (WeekStart = Monday). Server enforces ForecastRules.WeekIsSignable:
/// every elapsed weekday is approved, rejected-with-reason, or covered by an absence. Sign-off is
/// a marker over the approval state machine, never a second one.
/// </summary>
public sealed record SignOffLabourWeek(string WorkerId, DateTimeOffset WeekStart)
    : ICommand<LabourWeekSignOff>;

public sealed record RemoveLabourWeekSignOff(string WorkerId, DateTimeOffset WeekStart)
    : ICommand<Acknowledgement>;
