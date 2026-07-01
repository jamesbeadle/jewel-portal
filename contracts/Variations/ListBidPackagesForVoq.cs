using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

public sealed record ListBidPackagesForVoq(string VariationOrderQuoteId) : IQuery<IReadOnlyList<BidPackage>>;
