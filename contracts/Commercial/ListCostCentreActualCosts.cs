using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// The Xero purchase lines allocated to one project + cost centre — the detail
/// behind the Financials tab's actual cost figure. Net is sign-adjusted: supplier
/// credit notes (ACCPAYCREDIT) come back negative. Newest first.
/// </summary>
public sealed record ListCostCentreActualCosts(string ProjectId, string CostCode) : IQuery<IReadOnlyList<CostCentreActualCostLine>>;

public sealed record CostCentreActualCostLine(
    string XeroLedgerLineId,
    DateTime? Date,
    string Supplier,
    string InvoiceNumber,
    string Description,
    decimal Net);
