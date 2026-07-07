using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Per-cost-centre financial summary for one project's Financials tab.
/// Budgeted cost comes from the valuation report (every counting line —
/// contract works, provisional sums, contingency and variations; declined/TBC
/// lines excluded). Actual cost comes from Xero purchase lines allocated to
/// the project on the Allocation page (credit notes subtract). Other columns
/// (assigned budget from POs, completion, sales) come later.
/// </summary>
public sealed record GetProjectFinancialSummary(string ProjectId) : IQuery<IReadOnlyList<ProjectFinancialSummaryRow>>;

/// <summary>One cost centre's figures; only centres with a non-zero value are returned.</summary>
public sealed record ProjectFinancialSummaryRow(
    string CostCode,
    decimal BudgetedCost,
    decimal ActualCost);
