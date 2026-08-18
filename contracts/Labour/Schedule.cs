using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

// The per-worker monthly settlement schedule and its Dext/Xero coding contract (scope §6, §6a),
// plus the effective-dated Xero mapping admin (scope §3). CommercialTeam-gated throughout.

/// <summary>Every worker's settlement schedule for the month, with reconciliation verdicts.</summary>
public sealed record GetSettlementSchedules(int Year, int Month) : IQuery<SettlementScheduleSnapshot>;

/// <summary>
/// Adds a non-labour line (materials / travel) to a worker's month at sign-off level — these
/// change both the Xero account and the CIS treatment (no deduction). Labour lines are never
/// added here: they come from approved timesheets only.
/// </summary>
public sealed record AddWorkerSettlementLine(
    string WorkerId, int Year, int Month, string ProjectId, string CostCode,
    SettlementLineNature Nature, decimal Amount, string Note) : ICommand<Acknowledgement>;

public sealed record RemoveWorkerSettlementLine(string WorkerSettlementLineId) : ICommand<Acknowledgement>;

/// <summary>Both effective-dated Xero maps, current and historic rows.</summary>
public sealed record ListXeroMappings : IQuery<XeroMappingsSnapshot>;

/// <summary>
/// Points a project at a Xero site tracking option from now on. The previous row (if any) is
/// closed with EffectiveTo = now, never edited — historic reads keep translating through it.
/// </summary>
public sealed record SetSiteXeroMapping(string ProjectId, string XeroTrackingOptionId, string XeroTrackingOptionName)
    : ICommand<Acknowledgement>;

/// <summary>Same effective-dated contract for a cost code: its tracking option and the account
/// code per line nature (blank = the run's configured default for that nature).</summary>
public sealed record SetCostCodeXeroMapping(
    string CostCode, string XeroTrackingOptionId, string XeroTrackingOptionName,
    string LabourAccountCode, string MaterialsAccountCode, string TravelAccountCode)
    : ICommand<Acknowledgement>;

/// <summary>
/// The §6a automation, run per month (optionally narrowed to named workers). For each fully
/// signed-off worker-month it either recodes the covered draft bill to the schedule's split or
/// stages a draft bill, ALWAYS leaving the bill DRAFT in Xero — the accountant's approval there
/// remains the human gate. Mapping gaps skip-and-report; nothing is ever guessed. Every write
/// and every skip is recorded against the worker-month.
/// </summary>
public sealed record RunXeroCoding(int Year, int Month, IReadOnlyList<string>? WorkerIds)
    : ICommand<IReadOnlyList<XeroCodingRunResult>>;
