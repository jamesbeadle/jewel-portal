using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Restates the per-line % complete on a LOCKED claim (Preapproved or Confirmed) without moving
/// its total: money moves between the claim's own frozen rows, never in or out. The certified
/// total, the retention on it and the contract-side works (the deposit release's base) come out
/// to the penny as they were, or the restatement is refused. For the claim the client has paid
/// but whose lines were smeared (a variation re-spread at one uniform percentage), so the next
/// claim's "previous" and "this period" measure from the real position. A Draft's figures are
/// set with RecordClaimEntries, never restated.
/// </summary>
public sealed record RestateValuationClaimLines(
    string ValuationClaimId,
    IReadOnlyList<ClaimEntryInput> Entries) : ICommand<IReadOnlyList<ClaimLine>>;
