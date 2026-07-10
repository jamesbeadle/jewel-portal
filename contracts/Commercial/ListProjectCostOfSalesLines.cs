using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Every allocated Xero purchase line on the project — the WO Allocation tab's queue.
/// Whole-line allocations carry their centre and can be linked to a work order; a
/// split line appears once per share (IsSplit true, Net = the share, one row per
/// centre) and can't be linked — linking classifies the whole ledger line. Net is
/// sign-adjusted: supplier credit notes (ACCPAYCREDIT) come back negative. Newest first.
/// </summary>
public sealed record ListProjectCostOfSalesLines(string ProjectId) : IQuery<IReadOnlyList<ProjectCostOfSalesLine>>;

public sealed record ProjectCostOfSalesLine(
    string XeroLedgerLineId,
    DateTime? Date,
    string Supplier,
    string InvoiceNumber,
    string Description,
    string CostCode,
    decimal Net,
    bool IsSplit,
    IReadOnlyList<XeroWorkOrderLinkSlice>? WorkOrderLinks = null) // the order(s) this line pays against, with each one's share
{
    public IReadOnlyList<XeroWorkOrderLinkSlice> Links => WorkOrderLinks ?? Array.Empty<XeroWorkOrderLinkSlice>();
    public decimal LinkedTotal => Links.Sum(link => link.Amount);
    // The share of the line not yet paying any work order — non-work-order cost of
    // sales. Split shares can't carry links, so their whole net is unlinked.
    public decimal UnlinkedRemainder => Net - LinkedTotal;
}
