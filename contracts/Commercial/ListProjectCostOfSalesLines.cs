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
    string? LinkedWorkOrderId);
