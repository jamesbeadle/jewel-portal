using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

public sealed record StartValuationClaim(
    string ProjectId,
    int ClaimNumber,
    DateTimeOffset ClaimDate,
    decimal RetentionPercent,
    decimal RetentionReleasePercent) : ICommand<ValuationClaim>;
