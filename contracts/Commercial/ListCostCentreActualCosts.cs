using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// The Xero purchase lines allocated to one project + cost centre — the detail
/// behind the Financials tab's actual cost figure. Net is sign-adjusted: supplier
/// credit notes (ACCPAYCREDIT) come back negative. Newest first. A split line
/// contributes only its share of this centre (IsSplit true, Net = the share);
/// re-allocating a split is done from the allocation screen, not the move action.
/// </summary>
public sealed record ListCostCentreActualCosts(string ProjectId, string CostCode) : IQuery<IReadOnlyList<CostCentreActualCostLine>>;

public sealed record CostCentreActualCostLine(
    string XeroLedgerLineId,
    DateTime? Date,
    string Supplier,
    string InvoiceNumber,
    string Description,
    decimal Net,
    bool IsSplit = false,
    IReadOnlyList<XeroWorkOrderLinkSlice>? WorkOrderLinks = null) // the order(s) this line pays against, with each one's share
{
    public IReadOnlyList<XeroWorkOrderLinkSlice> Links => WorkOrderLinks ?? Array.Empty<XeroWorkOrderLinkSlice>();
    public decimal LinkedTotal => Links.Sum(link => link.Amount);
}
