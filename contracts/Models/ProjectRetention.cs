namespace Jewel.JPMS.Models;

// The two client-retention release events on a project: half the retention at practical
// completion, the balance once the defects period ends.
public enum RetentionMilestone
{
    Completion = 0,
    DefectsPeriodEnd = 1
}

// The project's client-retention terms plus the state of its two release milestones —
// added once per project (usually 5% / 2.5% / 6 or 12 months). Everything else (held to
// date, forecast release amounts, due dates) is CALCULATED from the valuation figures by
// RetentionSchedule; only what's been confirmed is stored, frozen at confirmation time.
// Distinct from SubcontractorRetention (what Jewel holds on subcontractors).
//
// DepositPercent is the cash-up-front deposit: the client pays deposit % of the contract
// sum before works start, and it is released back to them pro rata against each valuation's
// contract-side works (contract works + PC sums + contingency — variations excluded),
// reducing the payment due on every claim. 0 means no deposit on this project. Trailing
// default keeps the positional constructor stable for existing callers.
public sealed record ProjectRetention(
    string ProjectRetentionId,
    string ProjectId,
    decimal RetentionPercent,           // e.g. 5 — held on works complete until practical completion
    decimal CompletionReleasePercent,   // e.g. 2.5 — the first moiety, released at practical completion
    int DefectsPeriodMonths,            // 6 or 12 — the final release falls due this long after completion
    DateTimeOffset? PracticalCompletionAt,          // planned or achieved; anchors both due dates
    DateTimeOffset? CompletionReleaseConfirmedAt,   // null until someone confirms the release happened
    decimal CompletionReleaseAmount,                // frozen when confirmed; 0 until then
    DateTimeOffset? FinalReleaseConfirmedAt,
    decimal FinalReleaseAmount,
    decimal DepositPercent = 0m,        // e.g. 20 — cash up front, released back against contract-side works
    // Deposit releases settled before the portal began deducting them from claims (e.g.
    // Ravenswood's £6,049 covering claims 1–2, which were invoiced gross). Excluded from
    // every future claim's deduction so the portal reproduces the agreed client position.
    decimal DepositReleasedOpening = 0m);
