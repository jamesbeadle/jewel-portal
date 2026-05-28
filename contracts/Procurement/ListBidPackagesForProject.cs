using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

public sealed record ListBidPackagesForProject(string ProjectId) : IQuery<IReadOnlyList<BidPackage>>;
