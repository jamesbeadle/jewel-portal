using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

public sealed record ListClaimPeriodsForProject(string ProjectId) : IQuery<IReadOnlyList<ClaimPeriod>>;
