using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

public sealed record ListValuationClaimsForProject(string ProjectId) : IQuery<IReadOnlyList<ValuationClaim>>;
