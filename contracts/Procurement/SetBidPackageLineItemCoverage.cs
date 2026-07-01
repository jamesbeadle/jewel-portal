using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Link one bid package line item to its commercial home — exactly one of a contract BoQ line
// (Coverage = ContractLine, BoqLineItemId set) or a Variation Order Quote (Coverage = Variation,
// VariationOrderQuoteId set). Passing Coverage = Unassigned (with both ids null) clears the link.
// The handler enforces the one-of rule and that the referenced record exists on the package's project.
// Returns the package's full, ordered line-item list with the updated coverage.
public sealed record SetBidPackageLineItemCoverage(
    string LineItemId,
    BidPackageLineCoverage Coverage,
    string? BoqLineItemId = null,
    string? VariationOrderQuoteId = null) : ICommand<IReadOnlyList<BidPackageLineItem>>;
