using Jewel.JPMS.Api.Features.Procurement.Commands;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class ProcurementActions
{
    private static IEnumerable<AiAction> PackageScopeActions() => new AiAction[]
    {
        // ---- Package scope: line items, coverage, linked project documents ----------------------------------

        new AiAction(
            Name: "set_bid_package_line_items",
            Area: "Procurement",
            Description: "REPLACES the full set of scope line items on a bid package with the "
                + "supplied list — existing rows are deleted and recreated with new ids, which drops "
                + "their coverage links and quote-line references. Use add_bid_package_line_items to "
                + "append without touching existing rows. Returns the stored line items.",
            CommandType: typeof(SetBidPackageLineItems),
            ResultType: typeof(IReadOnlyList<BidPackageLineItem>),
            AuthorisationType: typeof(SetBidPackageLineItemsAuthorisation),
            ValidationType: typeof(SetBidPackageLineItemsValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "bidPackageId comes from list_bid_packages. Each line's costCode must be a code "
                + "in the cost-centre master list (list_cost_codes). Because this is a wholesale "
                + "replace, confirm with the user before calling on a package that already has "
                + "lines."),

        new AiAction(
            Name: "add_bid_package_line_items",
            Area: "Procurement",
            Description: "Appends scope line items to a bid package without touching the existing "
                + "set — existing rows keep their ids, coverage links and quote-line references "
                + "exactly as they stand. Returns the package's full stored line-item list.",
            CommandType: typeof(AddBidPackageLineItems),
            ResultType: typeof(IReadOnlyList<BidPackageLineItem>),
            AuthorisationType: typeof(AddBidPackageLineItemsAuthorisation),
            ValidationType: typeof(AddBidPackageLineItemsValidation),
            VisibleTo: PackageCreators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "bidPackageId comes from list_bid_packages. Each line's costCode must be a code "
                + "in the cost-centre master list (list_cost_codes)."),

        new AiAction(
            Name: "set_bid_package_line_item_coverage",
            Area: "Procurement",
            Description: "Links one bid package line item to its commercial home — a cost centre "
                + "(coverage ContractLine + costCode) or a variation order (coverage Variation + "
                + "variationOrderId), never both; coverage Unassigned clears the link. Returns the "
                + "package's full line-item list with the updated coverage.",
            CommandType: typeof(SetBidPackageLineItemCoverage),
            ResultType: typeof(IReadOnlyList<BidPackageLineItem>),
            AuthorisationType: typeof(SetBidPackageLineItemCoverageAuthorisation),
            ValidationType: typeof(SetBidPackageLineItemCoverageValidation),
            VisibleTo: PackageCreators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "lineItemId comes from the package's line items (get_bid_package_context). "
                + "boqLineItemId is legacy only — new contract-side links carry a cost centre."),

        new AiAction(
            Name: "set_bid_package_documents",
            Area: "Procurement",
            Description: "REPLACES the set of project documents (from the project's Documents "
                + "register — drawings, specifications, anything registered there) linked to a bid "
                + "package as the tender documents the invite email attaches, with the supplied "
                + "list — send the full desired set. Returns the linked documents, newest first.",
            CommandType: typeof(SetBidPackageDrawings),
            ResultType: typeof(IReadOnlyList<Drawing>),
            AuthorisationType: typeof(SetBidPackageDrawingsAuthorisation),
            ValidationType: typeof(SetBidPackageDrawingsValidation),
            VisibleTo: PackageAdministrators,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "bidPackageId comes from list_bid_packages. Wholesale replacement: omitting a "
                + "currently linked document unlinks it, so read the current set first "
                + "(get_bid_package_context). drawingIds keeps the register's old parameter name."),

    };
}
