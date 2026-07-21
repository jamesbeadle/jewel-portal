using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

// Sets a claim's free-text period name (e.g. "June 2026"). Allowed at any status —
// naming is bookkeeping, not a financial change, so a locked claim may still be renamed.
public sealed record RenameValuationClaim(
    string ValuationClaimId,
    string Name) : ICommand<ValuationClaim>;
