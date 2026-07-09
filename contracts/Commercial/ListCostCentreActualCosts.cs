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
    string? LinkedWorkOrderId = null); // the work order this line pays against, if linked
