namespace Jewel.JPMS.Api.Features.Commercial.Documents;

/// <summary>
/// Everything the cost-centre reconciliation PDF needs: the project identity for the header,
/// the centre (or roll-up group) being reconciled, and the already-computed detail. Assembled
/// by <see cref="CostCentreReconciliationPdfBuilder"/>; the derived figures live here so the
/// PDF and the on-screen modal share one set of definitions.
/// </summary>
public sealed record CostCentreReconciliationDocument(
    string ProjectReference,
    string ProjectName,
    string ClientName,
    string Heading,
    IReadOnlyList<string> CostCodes,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<ReconciliationSalesLine> SalesLines,
    // Bridge: BudgetedSales less the listed lines (rounding, unlisted adjustments). Zero almost always.
    decimal SalesOther,
    IReadOnlyList<ReconciliationWorkOrderLine> WorkOrders,
    IReadOnlyList<ReconciliationCostLine> XeroCosts,
    decimal LabourCost,
    // Bridge: NonWorkOrderActualCost less labour less the listed Xero lines (splits, re-attributions).
    decimal OtherAdjustments,
    decimal SalesValue,
    decimal TargetCost,
    decimal WoCommitted,
    decimal NonWoCost)
{
    public decimal TotalCosts => WoCommitted + NonWoCost;
    public decimal GrossProfit => SalesValue - TotalCosts;
    /// <summary>Buying gain: what the scope was budgeted to cost less what it is costing.</summary>
    public decimal ProcurementGainLoss => TargetCost - TotalCosts;
    public decimal? MarginPercent => SalesValue == 0m ? null : Math.Round(GrossProfit / SalesValue * 100m, 1);
}

public sealed record ReconciliationSalesLine(string Reference, string Description, decimal Amount);

public sealed record ReconciliationWorkOrderLine(
    string Reference, string Supplier, string Title, string Status, decimal Amount);

public sealed record ReconciliationCostLine(
    string Date, string Supplier, string InvoiceNumber, string Description, decimal Amount);
