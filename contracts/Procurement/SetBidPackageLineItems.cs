using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// One scope line as supplied by the caller (no id — ids are assigned server-side). CostCode is
// required and must be a code in the cost-centre master list — every line put out to tender knows
// its cost-centre home up front.
public sealed record BidPackageLineItemInput(
    string Description,
    string Unit,
    decimal Quantity,
    string Trade,
    string CostCode);

// Replace the full set of line items on a bid package with the supplied list (order preserved as
// SortOrder). Returns the stored line items.
public sealed record SetBidPackageLineItems(
    string BidPackageId,
    IReadOnlyList<BidPackageLineItemInput> LineItems) : ICommand<IReadOnlyList<BidPackageLineItem>>;
