using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Creates a bid package under a VOQ (linked via BidPackage.VariationOrderQuoteId) and moves the VOQ
/// to Inviting if it was still Draft. The package then uses the normal procurement invite/tender flow.
/// </summary>
public sealed record AddBidPackageToVoq(
    string VariationOrderQuoteId,
    string Title,
    string Trade,
    string OwnerEmail) : ICommand<BidPackage>;
