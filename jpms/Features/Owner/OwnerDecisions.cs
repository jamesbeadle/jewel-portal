using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Features.Cvr;
using static Jewel.JPMS.Features.Cashflow.CashflowDisplay;
using static Jewel.JPMS.Features.Cvr.ProfitDisplay;

namespace Jewel.JPMS.Features.Owner;

/// <summary>One thing the board should look at, with where to look.</summary>
public sealed record OwnerDecision(string Title, string Detail, string Href, bool IsSerious);

/// <summary>
/// The "needs a decision" list, read off figures the finance pages already calculate. Every
/// test is a figure against ZERO; a threshold or cash buffer is a definition the board has not
/// made yet (OwnerGapsPanel), and a made-up one would be a false alarm or a false comfort.
/// </summary>
public static class OwnerDecisions
{
    private const decimal Nothing = 0m;

    public static IReadOnlyList<OwnerDecision> For(
        IReadOnlyList<(Project Project, ProfitRow Row)>? profitRows,
        WeeklyCashflowView? weekly,
        decimal? overduePayables,
        decimal? overdueReceivables)
    {
        var decisions = new List<OwnerDecision>();

        if (weekly is { Closing: { } closing } && closing.Count > 0)
        {
            var lowIndex = weekly.MinClosingIndex;
            var low = closing[lowIndex];
            if (low < Nothing)
                decisions.Add(new OwnerDecision(
                    "The 13-week plan goes below zero",
                    $"{Money(low)} in the week of {WeekLabel(weekly.WeekStarts[lowIndex], lowIndex)}, on the recorded bills, invoices and manual items.",
                    "/finance/weekly-cashflow", IsSerious: true));
        }

        foreach (var (project, row) in profitRows ?? Array.Empty<(Project, ProfitRow)>())
        {
            var wholeJobLoss = row.ForecastedProfit < Nothing;
            var lossOnTheRemainder = !wholeJobLoss && row.ToFinishProfit < Nothing;
            if (wholeJobLoss)
                decisions.Add(new OwnerDecision(
                    $"{project.Name}: forecast whole-job loss",
                    $"{Money(row.ForecastedProfit)} at completion — {MoneyCompact(row.ContractValue)} contract against {MoneyCompact(row.ForecastCostOfSales)} forecast cost.",
                    $"/projects/{project.ProjectId}/financials", IsSerious: true));
            if (lossOnTheRemainder)
                decisions.Add(new OwnerDecision(
                    $"{project.Name}: loss on the work remaining",
                    $"{Money(row.ToFinishProfit)} between {MoneyCompact(row.LeftToCertify)} left to certify and {MoneyCompact(row.CostToComplete)} cost to complete; the job is still {Money(row.ForecastedProfit)} up overall.",
                    $"/projects/{project.ProjectId}/financials", IsSerious: false));
        }

        if (overduePayables is { } payables && payables > Nothing)
            decisions.Add(new OwnerDecision(
                "Supplier bills past their due date",
                $"{Money(payables)} overdue, drafts included — timing and disputes to settle, not necessarily all to pay.",
                "/finance/aged-payables", IsSerious: false));

        if (overdueReceivables is { } receivables && receivables > Nothing)
            decisions.Add(new OwnerDecision(
                "Client invoices past their due date",
                $"{Money(receivables)} overdue from clients.",
                "/finance/aged-receivables", IsSerious: false));

        return decisions;
    }
}
