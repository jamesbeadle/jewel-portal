using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Commercial;

// The project's drawdown — the funds still available across the job — calculated exactly as
// the Financials tab's Total Drawdown so every tab that shows it agrees to the penny. It is
// each cost centre's unspent target cost (target cost, packaged scope netted off, less
// non-work-order spend and committed work orders), with two rules a naive project-level
// "target − orders − spend" subtraction misses:
//
//   * Finalised (locked) centres drop out. Locking a line banks its remaining budget as
//     realised profit / loss, so it is no longer available drawdown.
//   * Reconciliation packages carry their own drawdown. Their scope is netted off the
//     centres (via the Packaged* figures) and each unlocked package's drawdown is added
//     back, so packaged money is counted once — on the package.
//
// Mirrors FinancialsTable's TotalDrawdown (hide-packaged-scope on, the displayed default);
// the Cashflow and Financials tabs both read the number from here, so keep them in step.
public static class ProjectDrawdown
{
    // Committed work-order value per cost code: the totals of order lines that carry a cost
    // code (lines without one can't land on a Financials row, so they're excluded — matching
    // the Financials tab). Drafts COUNT: a draft is an intended commitment being written up,
    // and the business wants the Financials tab to show the position including them. Rejected
    // drafts count nowhere — the decision was no. (Cancelled orders keep their long-standing
    // inclusion here; changing that is a separate decision.) Case-insensitive keys, like
    // every cost-code lookup.
    public static IReadOnlyDictionary<string, decimal> CommittedByCostCode(
        IEnumerable<ProjectWorkOrderDetail> workOrders) =>
        workOrders
            .Where(detail => !detail.Order.IsRejected)
            .SelectMany(detail => detail.Lines)
            .Where(line => !string.IsNullOrWhiteSpace(line.CostCode))
            .GroupBy(line => line.CostCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key,
                          group => group.Sum(line => line.LineTotal),
                          StringComparer.OrdinalIgnoreCase);

    // The whole-project drawdown, netted: drawdown less overspend. Kept for the Cashflow tab's
    // single "Cost centre drawdowns" row; the Financials tab shows the two sides separately
    // (SplitForProject), and the two agree by construction: ForProject == Split.Net.
    public static decimal ForProject(
        IEnumerable<ProjectFinancialSummaryRow> summaryRows,
        IReadOnlyDictionary<string, decimal> committedByCostCode,
        IEnumerable<PackageReconciliationRow> packages) =>
        SplitForProject(summaryRows, committedByCostCode, packages).Net;

    // The same remainders split by sign, per cost centre (finance meeting 2026-08-03): a centre
    // with target cost still unspent contributes to Drawdown (positive only); a centre whose
    // committed cost of sales has passed its target contributes to Overspend (negative only).
    // Split PER CENTRE, not on the netted total — one underspent centre must not hide another's
    // overspend. summaryRows is the per-cost-centre financial summary; committedByCostCode is
    // CommittedByCostCode(...) for the project's work orders; packages are the
    // reconciliation-package rows (from ListPackageReconciliation).
    public static DrawdownSplit SplitForProject(
        IEnumerable<ProjectFinancialSummaryRow> summaryRows,
        IReadOnlyDictionary<string, decimal> committedByCostCode,
        IEnumerable<PackageReconciliationRow> packages)
    {
        var byCode = summaryRows.ToDictionary(row => row.CostCode, StringComparer.OrdinalIgnoreCase);

        // Every code with a figure on either side — summary centres plus work-order-only codes.
        var codes = byCode.Keys.Union(committedByCostCode.Keys, StringComparer.OrdinalIgnoreCase);

        var drawdown = 0m;
        var overspend = 0m;
        foreach (var code in codes)
        {
            var hasRow = byCode.TryGetValue(code, out var row);

            // Finalised centres are realised to profit / loss, not drawdown.
            if (hasRow && row!.IsFinalised) continue;

            // Target cost with packaged sales netted off (the package carries that scope).
            // BudgetedCost is BudgetedSales x CostFactor, so this rebuilds it net of packages.
            var target = hasRow
                ? Math.Round((row!.BudgetedSales - row.PackagedSales) * FinancialSummaryAssumptions.CostFactor, 2)
                : 0m;

            var nonWoSpend = hasRow ? row!.NonWorkOrderActualCost - row.PackagedNonWoCost : 0m;
            var packagedWo = hasRow ? row!.PackagedWoCommitted : 0m;
            var committed = committedByCostCode.TryGetValue(code, out var value) ? value : 0m;

            // Committed includes packaged orders' value; add the packaged slice back, because
            // the package row already accounts for it.
            var remainder = target - nonWoSpend - committed + packagedWo;
            if (remainder > 0m) drawdown += remainder; else overspend += remainder;
        }

        // Each unlocked package's own drawdown (target cost less committed), sign-split like a
        // centre. Locked packages froze their figures into profit / loss at lock, so they add
        // nothing here.
        foreach (var package in packages.Where(package => !package.IsLocked))
        {
            if (package.Drawdown > 0m) drawdown += package.Drawdown; else overspend += package.Drawdown;
        }

        return new DrawdownSplit(drawdown, overspend);
    }

    // Forecasted Cost of Sales for the whole project: committed cost of sales (work orders +
    // non-WO spend, packaged scope on the packages) plus the drawdown still to be spent —
    // an underspent line is forecast to spend its full target cost, an overspent one its
    // committed cost. Mirrors the Financials tab's Forecasted Cost of Sales total (packaged
    // scope netted, the displayed default) so the Profit Summary reads the same number.
    public static decimal ForecastCostOfSales(
        IEnumerable<ProjectFinancialSummaryRow> summaryRows,
        IReadOnlyDictionary<string, decimal> committedByCostCode,
        IEnumerable<PackageReconciliationRow> packages)
    {
        var rows = summaryRows as IReadOnlyList<ProjectFinancialSummaryRow> ?? summaryRows.ToList();
        var packageRows = packages as IReadOnlyList<PackageReconciliationRow> ?? packages.ToList();

        var committed =
            committedByCostCode.Values.Sum()
            - rows.Sum(row => row.PackagedWoCommitted)
            + rows.Sum(row => row.NonWorkOrderActualCost - row.PackagedNonWoCost)
            + packageRows.Where(package => !package.IsLocked).Sum(package => package.TargetCost - package.Drawdown);

        return committed + SplitForProject(rows, committedByCostCode, packageRows).Drawdown;
    }
}

/// <summary>
/// The project's target-cost remainders split by sign: <see cref="Drawdown"/> is the budget
/// still available to spend (positive only, summed per centre), <see cref="Overspend"/> what
/// committed cost has already passed target (negative only). <see cref="Net"/> is the old
/// netted drawdown figure — the Cashflow tab's row.
/// </summary>
public sealed record DrawdownSplit(decimal Drawdown, decimal Overspend)
{
    public decimal Net => Drawdown + Overspend;
}
