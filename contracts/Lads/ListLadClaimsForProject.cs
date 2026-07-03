using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Lads;

public sealed record ListLadClaimsForProject(string ProjectId) : IQuery<IReadOnlyList<LadClaim>>;
