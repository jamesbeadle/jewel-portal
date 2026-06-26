using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

// Client has paid. Freezes the summary totals and the per-row claimed amounts, advancing
// CertifiedToDate so the next claim measures its increment from here.
public sealed record ConfirmValuationClaim(string ValuationClaimId) : ICommand<ValuationClaim>;
