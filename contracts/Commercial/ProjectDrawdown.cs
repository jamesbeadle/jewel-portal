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
    // the Financials tab). Case-insensitive keys, like every cost-code lookup.
    public static IReadOnlyDictionary<string, decimal> CommittedByCostCode(
        IEnumerable<ProjectWorkOrderDetail> workOrders) =>
        workOrders
            .SelectMany(detail => detail.Lines)
            .Where(line => !string.IsNullOrWhiteSpace(line.CostCode))
            .GroupBy(line => line.CostCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key,
                          group => group.Sum(line => line.LineTotal),
                          StringComparer.OrdinalIgnoreCase);

    // The whole-project drawdown. summaryRows is the per-cost-centre financial summary;
    // committedByCostCode is CommittedByCostCode(...) for the project's work orders; packages
    // are the reconciliation-package rows (from ListPackageReconciliation).
    public static decimal ForProject(
        IEnumerable<ProjectFinancialSummaryRow> summaryRows,
        IReadOnlyDictionary<string, decimal> committedByCostCode,
        IEnumerable<PackageReconciliationRow> packages)
    {
        var byCode = summaryRows.ToDictionary(row => row.CostCode, StringComparer.OrdinalIgnoreCase);

        // Every code with a figure on either side — summary centres plus work-order-only codes.
        var codes = byCode.Keys.Union(committedByCostCode.Keys, StringComparer.OrdinalIgnoreCase);

        var centres = 0m;
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
            centres += target - nonWoSpend - committed + packagedWo;
        }

        // Each unlocked package's own drawdown (target cost less committed). Locked packages
        // froze their figures into profit / loss at lock, so they add nothing here.
        var packageDrawdown = packages.Where(package => !package.IsLocked).Sum(package => package.Drawdown);

        return centres + packageDrawdown;
    }
}
