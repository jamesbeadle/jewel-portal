using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

public sealed record ListClaimLines(string ValuationClaimId) : IQuery<IReadOnlyList<ClaimLine>>;
