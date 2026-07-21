using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Commercial;

// Deletes a claim and its per-line entries (test claims, false starts). Valuation
// invoices and snapshots that referenced it survive with the link cleared — money
// already invoiced/certified is history and must not move when a claim is removed.
public sealed record DeleteValuationClaim(
    string ValuationClaimId) : ICommand<Acknowledgement>;
