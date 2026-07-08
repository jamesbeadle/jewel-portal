using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Per-cost-centre financial summary for one project's Financials tab.
/// Budgeted sales is the valuation report's counting lines per cost centre
/// (contract works, provisional sums, contingency and variations; declined/TBC
/// lines excluded) — what we bill the client. Budgeted cost is budgeted sales
/// less the assumed margin (<see cref="FinancialSummaryAssumptions.MarginPercent"/>).
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
    decimal BudgetedSales,
    decimal BudgetedCost,        // BudgetedSales less the assumed margin
    decimal CompletionPercent,   // 0–100, from the latest claim (any status)
    decimal ExpectedActualCost,  // BudgetedCost × CompletionPercent
    decimal ActualCost,
    decimal UnderOverExpected);  // ExpectedActualCost − ActualCost; positive = under

/// <summary>Assumptions shared by the API calculation and the UI's explanation of it.</summary>
public static class FinancialSummaryAssumptions
{
    /// <summary>Assumed margin between budgeted sales and budgeted cost. Fixed for now;
    /// make configurable per project/cost centre when the business needs it.</summary>
    public const decimal MarginPercent = 10m;

    /// <summary>The cost fraction of a sales figure, e.g. 0.9 for a 10% margin.</summary>
    public const decimal CostFactor = 1m - MarginPercent / 100m;
}
