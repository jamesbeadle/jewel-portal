using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Link one bid package line item to its commercial home — a COST CENTRE (Coverage = ContractLine,
// CostCode set: the contract-side home the line's committed value charges to) or a VARIATION ORDER
// (Coverage = Variation, VariationOrderId set) — never both. Passing Coverage = Unassigned clears
// the link. BoqLineItemId survives for LEGACY links only: linking by BoQ line retired 2026-08-16
// (packages rarely had BoQ lines to link to), and new contract-side links carry a cost centre.
// The handler enforces the one-of rule and that the referenced record exists.
// Returns the package's full, ordered line-item list with the updated coverage.
public sealed record SetBidPackageLineItemCoverage(
    string LineItemId,
    BidPackageLineCoverage Coverage,
    string? BoqLineItemId = null,
    string? VariationOrderId = null,
    string? CostCode = null) : ICommand<IReadOnlyList<BidPackageLineItem>>;
