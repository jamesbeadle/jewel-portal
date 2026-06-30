using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// The scope lines on a bid package, ordered for display (grouped by trade/speciality).
public sealed record ListBidPackageLineItems(string BidPackageId) : IQuery<IReadOnlyList<BidPackageLineItem>>;
