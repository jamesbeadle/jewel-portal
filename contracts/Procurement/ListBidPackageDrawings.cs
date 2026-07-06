using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// The project drawings linked to a bid package — its tender documents.
public sealed record ListBidPackageDrawings(
    string BidPackageId) : IQuery<IReadOnlyList<Drawing>>;
