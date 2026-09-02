namespace Jewel.JPMS.Features.Cvr;

/// <summary>One project's figures on the Profit Summary — the deal, the current position, what is
/// left to finish, and the forecast at completion — built by the page, rendered by the table's rows.</summary>
public sealed record ProfitRow(
    decimal InitialContractSum,
    decimal InitialContractCosts,
    decimal NetVariations,
    decimal CertifiedToDate,
    decimal ActualCostOfSales,
    decimal ContractValue,
    decimal ForecastCostOfSales,
    decimal WorksComplete,
    decimal RetentionOutstanding)
{
    public decimal BudgetedProfit => InitialContractSum - InitialContractCosts;
    public decimal CurrentProfit => CertifiedToDate - ActualCostOfSales;
    public decimal ForecastedProfit => ContractValue - ForecastCostOfSales;

    // The "To finish" band: what is left in the job from today. Revenue still to certify,
    // cost still to come, and the profit between them — defined so that
    // CurrentProfit + ToFinishProfit = ForecastedProfit exactly.
    public decimal LeftToCertify => ContractValue - CertifiedToDate;
    public decimal CostToComplete => ForecastCostOfSales - ActualCostOfSales;
    public decimal ToFinishProfit => LeftToCertify - CostToComplete;
    public decimal? ToFinishMargin => MarginOn(ToFinishProfit, LeftToCertify);

    // The bridge's third bar, defined so Budgeted + Variations + CostMovement = Forecast
    // exactly (ContractValue = InitialContractSum + NetVariations makes the algebra close).
    // Negative means the job is forecast to cost more than the deal's target.
    public decimal CostMovement => InitialContractCosts - ForecastCostOfSales;

    // How far the landing has moved from the deal — the "biggest swing" tile.
    public decimal ForecastSwing => ForecastedProfit - BudgetedProfit;

    // Value of work done against the revised sum — the project cell's progress bar. Null
    // when there is no contract value yet: no base, no honest percentage.
    public decimal? PercentComplete => ContractValue == 0m ? null : WorksComplete / ContractValue;

    // Margins on value: each profit against its own revenue column — NOT markup on cost —
    // so the budgeted margin reads ≈9.1% for the assumed 10% markup, by design (see
    // FinancialSummaryAssumptions.CostFactor's remark on the distinction). Null when the
    // revenue base is zero: no base, no honest percentage — the cell's % line simply
    // doesn't render, like any figure that isn't known.
    public decimal? BudgetedMargin => MarginOn(BudgetedProfit, InitialContractSum);
    public decimal? CurrentMargin => MarginOn(CurrentProfit, CertifiedToDate);
    public decimal? ForecastedMargin => MarginOn(ForecastedProfit, ContractValue);

    private static decimal? MarginOn(decimal profit, decimal revenue) =>
        revenue == 0m ? null : profit / revenue;

    /// <summary>The selection summed: every input added up, every derived figure following.</summary>
    public static ProfitRow TotalOf(IReadOnlyList<(Project Project, ProfitRow Row)> rows) =>
        new(
            rows.Sum(entry => entry.Row.InitialContractSum),
            rows.Sum(entry => entry.Row.InitialContractCosts),
            rows.Sum(entry => entry.Row.NetVariations),
            rows.Sum(entry => entry.Row.CertifiedToDate),
            rows.Sum(entry => entry.Row.ActualCostOfSales),
            rows.Sum(entry => entry.Row.ContractValue),
            rows.Sum(entry => entry.Row.ForecastCostOfSales),
            rows.Sum(entry => entry.Row.WorksComplete),
            rows.Sum(entry => entry.Row.RetentionOutstanding));
}
