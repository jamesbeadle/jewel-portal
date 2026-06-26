using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

// Set / update the cumulative % complete for one line within a Draft claim.
// The handler upserts the ClaimLine and recomputes CumulativeClaimed and PeriodIncrement.
public sealed record RecordClaimEntry(
    string ValuationClaimId,
    string ValuationLineItemId,
    decimal PercentComplete) : ICommand<ClaimLine>;
