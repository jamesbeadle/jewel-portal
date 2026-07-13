using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Per-cost-centre financial summary for one project's Financials tab.
/// Budgeted sales is the valuation report's counting lines per cost centre
/// (contract works, provisional sums, contingency and variations; declined/TBC
/// lines excluded) — what we bill the client. Budgeted cost is budgeted sales
/// with the assumed markup backed out (<see cref="FinancialSummaryAssumptions.MarkupPercent"/>).
/// Completion % is the latest claim's cumulative claimed value against budgeted
/// sales; expected actual cost applies that completion to the budgeted cost.
/// Actual cost comes from Xero purchase lines allocated to the project on the
/// Allocation page (credit notes subtract). Other columns (assigned budget from
/// POs, sales-side actuals) come later.
/// </summary>
public sealed record GetProjectFinancialSummary(string ProjectId) : IQuery<IReadOnlyList<ProjectFinancialSummaryRow>>;

/// <summary>One cost centre's figures; only centres with a non-zero value are returned.</summary>
public sealed record ProjectFinancialSummaryRow(
    string CostCode,
    decimal BudgetedSales,       // contract sales value: counting valuation lines (contract + variations)
    decimal BudgetedCost,        // BudgetedSales ÷ (1 + assumed markup) — the target cost
    decimal CompletionPercent,   // 0–100, sales-side, from the latest claim (any status; edited on the valuation)
    decimal ExpectedActualCost,  // BudgetedCost × CompletionPercent
    decimal ActualCost,
    decimal UnderOverExpected,   // ExpectedActualCost − ActualCost; positive = under
    decimal ClaimedToDate = 0m,          // claim value: the latest claim's cumulative claimed £ for this centre
    decimal CostCompletionPercent = 0m,  // 0–100, cost-side, edited inline on the Financials tab
    decimal NonWorkOrderActualCost = 0m, // allocated Xero spend not linked to any work order
    bool IsFinalised = false,            // locked down: drawdown reads as realised profit / loss
    decimal LabourActualCost = 0m,       // approved timesheet labour + settlement variances (inside ActualCost and Non-WO)
    decimal PendingLabourCost = 0m);     // submitted-not-yet-approved hours x current rate: visible, never posted

/// <summary>Assumptions shared by the API calculation and the UI's explanation of it.</summary>
public static class FinancialSummaryAssumptions
{
    /// <summary>Assumed markup applied to cost to reach the sales figure we bill:
    /// target cost × (1 + markup) = contract sales value. Fixed for now; make
    /// configurable per project/cost centre when the business needs it.</summary>
    public const decimal MarkupPercent = 10m;

    /// <summary>The cost fraction of a sales figure: 1 ÷ (1 + markup), ≈0.9091 for a 10%
    /// markup — so the target cost plus 10% gets back to the sales figure exactly.
    /// (Not 1 − 10%: that would treat the 10% as a margin on sales, understating cost.)</summary>
    public const decimal CostFactor = 1m / (1m + MarkupPercent / 100m);
}
