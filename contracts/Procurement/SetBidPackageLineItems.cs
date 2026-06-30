using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// One scope line as supplied by the caller (no id — ids are assigned server-side).
public sealed record BidPackageLineItemInput(
    string Description,
    string Unit,
    decimal Quantity,
    string Trade);

// Replace the full set of line items on a bid package with the supplied list (order preserved as
// SortOrder). Returns the stored line items.
public sealed record SetBidPackageLineItems(
    string BidPackageId,
    IReadOnlyList<BidPackageLineItemInput> LineItems) : ICommand<IReadOnlyList<BidPackageLineItem>>;
