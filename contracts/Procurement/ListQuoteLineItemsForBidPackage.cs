using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// All quote lines across every quote on the package — the comparison view joins these to the
// package's line items (via BidPackageLineItemId) to show subbies' rates side by side.
public sealed record ListQuoteLineItemsForBidPackage(
    string BidPackageId) : IQuery<IReadOnlyList<QuoteLineItem>>;
