using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

// Worker ↔ directory linking and the chase-list dismissals (2026-08-31, the accountant's
// month-end doc items A–H): settlement is gated on the worker's counterparty — a linked
// subcontractor company, or the worker themself when they are a flagged sole trader — and until
// now the ONLY way to set the link was the Workers modal, one worker at a time. These commands
// make the link settable where the gap is found (the allocation page's warning, the Workers
// page's matching card, the connector), make the Xero import create it automatically, and let a
// wrong chase item be dismissed with a reason instead of haunting the list forever.

/// <summary>
/// Sets one worker's settlement identity in a single act: the linked subcontractor company
/// (null clears the link) and the sole-trader flag. The company link always wins where both are
/// set; the flag exists precisely so a sole trader needs no invented directory company.
/// </summary>
public sealed record SetWorkerSettlementIdentity(
    string WorkerId,
    string? SubcontractorId,
    bool IsSoleTrader) : ICommand<Worker>;

/// <summary>
/// The connector's by-name link: joins a worker to a directory company, both matched by name
/// server-side (unambiguous or refused with the candidates).
/// </summary>
public sealed record LinkWorkerToCompanyByName(
    string WorkerName,
    string CompanyName) : ICommand<Worker>;

/// <summary>The connector's by-name sole-trader toggle.</summary>
public sealed record SetWorkerSoleTraderByName(
    string WorkerName,
    bool IsSoleTrader) : ICommand<Worker>;

/// <summary>
/// Matches every active, unlinked, non-sole-trader worker against the company directory by name
/// (the same normalised matching the Xero labour recognition uses). Apply=false reports what
/// WOULD link without writing; Apply=true writes the unambiguous links (audited per worker) and
/// still reports the ambiguous and unmatched remainder for a human decision.
/// </summary>
public sealed record ReconcileWorkerDirectoryLinks(
    bool Apply,
    string LinkedByEmail = "") : ICommand<WorkerDirectoryLinkReport>;

public sealed record WorkerDirectoryLinkCandidate(
    string SubcontractorId,
    string CompanyName);

/// <summary>One worker's matching outcome: Linked ("linked"/"would link" per Apply) with the
/// company, Ambiguous with its candidates, or Unmatched — the human decides those (link by hand,
/// or flag the worker a sole trader).</summary>
public sealed record WorkerDirectoryLinkOutcome(
    string WorkerId,
    string WorkerName,
    string Outcome,
    WorkerDirectoryLinkCandidate? Linked,
    IReadOnlyList<WorkerDirectoryLinkCandidate> Candidates);

public sealed record WorkerDirectoryLinkReport(
    bool Applied,
    IReadOnlyList<WorkerDirectoryLinkOutcome> Workers);

/// <summary>
/// Dismisses one worker's chase-list day with a mandatory reason — the day was reviewed and
/// needs no timesheet and no absence. The day leaves the chase list AND the unconfirmed-cost
/// accrual, and the dismissal is written to the audit trail. A timesheet or absence recorded
/// later supersedes it naturally.
/// </summary>
public sealed record DismissLabourChaseDay(
    string WorkerId,
    DateTimeOffset Date,
    string Reason,
    string DismissedByEmail = "") : ICommand<Acknowledgement>;

/// <summary>The connector's by-name dismissal.</summary>
public sealed record DismissLabourChaseDayByName(
    string WorkerName,
    DateTimeOffset Date,
    string Reason,
    string DismissedByEmail = "") : ICommand<Acknowledgement>;

/// <summary>Removes a dismissal, putting the day back on the chase list (by name, connector).</summary>
public sealed record RestoreLabourChaseDayByName(
    string WorkerName,
    DateTimeOffset Date) : ICommand<Acknowledgement>;
