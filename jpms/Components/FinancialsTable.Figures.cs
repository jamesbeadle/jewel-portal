using Jewel.JPMS.Commercial;

namespace Jewel.JPMS.Components;

public partial class FinancialsTable
{
    private decimal RowTarget(ProjectFinancialSummaryRow row) =>
        hidePackagedScope
            ? Math.Round((row.BudgetedSales - row.PackagedSales) * FinancialSummaryAssumptions.CostFactor, 2)
            : row.BudgetedCost;

    private decimal RowActual(ProjectFinancialSummaryRow row) =>
        row.ActualCost - (hidePackagedScope ? row.PackagedActualCost : 0m);

    // Direct (non-WO) purchase slices inside packages net out of the Non-WO column too.
    private decimal RowNonWo(ProjectFinancialSummaryRow row) =>
        row.NonWorkOrderActualCost - (hidePackagedScope ? row.PackagedNonWoCost : 0m);

    private decimal PackagedWoFor(string costCode) =>
        hidePackagedScope && byCode.TryGetValue(costCode, out var row) ? row.PackagedWoCommitted : 0m;

    private decimal ContractSalesFor(IReadOnlyList<string> costCodes) => SumFor(costCodes, RowSales);

    private decimal ClaimValueFor(IReadOnlyList<string> costCodes) => SumFor(costCodes, RowClaimed);

    // Contract sales value with the markup backed out — what the line should cost us.
    private decimal TargetCostFor(IReadOnlyList<string> costCodes) => SumFor(costCodes, RowTarget);

    // Value-weighted across the line's centres: claimed £ over contract £.
    private decimal SalesCompletionFor(IReadOnlyList<string> costCodes)
    {
        var sales = ContractSalesFor(costCodes);
        return sales == 0m ? 0m : Math.Round(ClaimValueFor(costCodes) / sales * 100m, 1);
    }

    // Weighted by target cost; falls back to a simple average when the line carries no target.
    private decimal CostCompletionFor(IReadOnlyList<string> costCodes)
    {
        var target = TargetCostFor(costCodes);
        if (target != 0m)
            return Math.Round(SumFor(costCodes, row => row.CostCompletionPercent * RowTarget(row)) / target, 1);
        var percents = costCodes
            .Select(entryCode => byCode.TryGetValue(entryCode, out var row) ? row.CostCompletionPercent : 0m)
            .ToList();
        return percents.Count == 0 ? 0m : Math.Round(percents.Average(), 1);
    }

    private decimal WoCommittedFor(IReadOnlyList<string> costCodes) =>
        costCodes.Sum(entryCode =>
            (WorkOrderCommittedByCode.TryGetValue(entryCode, out var committed) ? committed : 0m)
            - PackagedWoFor(entryCode));

    // Allocated Xero purchase spend, net of credit notes.
    private decimal CostOfSalesFor(IReadOnlyList<string> costCodes) => SumFor(costCodes, RowActual);

    // The share of that spend not linked to any work order.
    private decimal NonWoCostOfSalesFor(IReadOnlyList<string> costCodes) => SumFor(costCodes, RowNonWo);

    // Committed Cost of Sales: committed work orders plus unlinked actual spend — what is
    // already committed against the line. Target Cost Value − Committed splits by sign into
    // Drawdown / Overspend.
    private decimal CommittedFor(IReadOnlyList<string> costCodes) =>
        WoCommittedFor(costCodes) + NonWoCostOfSalesFor(costCodes);

    // Forecasted Cost of Sales: committed plus the drawdown still to be spent — the line's
    // expected total cost. An underspent line forecasts its full target; an overspent one
    // its committed cost.
    private decimal ForecastFor(IReadOnlyList<string> costCodes) =>
        CommittedFor(costCodes) + DrawdownFor(costCodes);

    private bool IsCentreFinalised(string costCode) =>
        byCode.TryGetValue(costCode, out var row) && row.IsFinalised;

    private bool AllFinalised(IReadOnlyList<string> costCodes) => costCodes.All(IsCentreFinalised);

    private bool AnyFinalised(IReadOnlyList<string> costCodes) => costCodes.Any(IsCentreFinalised);

    // What one centre has left of its target cost after work orders and unlinked spend
    // (all netted of packaged scope when the toggle is on).
    private decimal RemainderFor(string costCode) =>
        (byCode.TryGetValue(costCode, out var row) ? RowTarget(row) - RowNonWo(row) : 0m)
        - (WorkOrderCommittedByCode.TryGetValue(costCode, out var committed) ? committed : 0m)
        + PackagedWoFor(costCode);

    // Unlocked centres' remainders split by sign, PER CENTRE — not net-then-split, so one
    // underspent centre in a roll-up cannot hide another's overspend. Drawdown is the funds
    // still available on the line; Overspend the committed cost already past target.
    private decimal DrawdownFor(IReadOnlyList<string> costCodes) =>
        costCodes.Where(entryCode => !IsCentreFinalised(entryCode)).Sum(entryCode => Math.Max(0m, RemainderFor(entryCode)));

    private decimal OverspendFor(IReadOnlyList<string> costCodes) =>
        costCodes.Where(entryCode => !IsCentreFinalised(entryCode)).Sum(entryCode => Math.Min(0m, RemainderFor(entryCode)));

    // What one centre's sales value leaves after work orders and unlinked spend (netted of
    // packaged scope like everything else). Same cost side as RemainderFor, but measured
    // against Contract Sales Value rather than target cost — profit is what the line sold
    // for less what it cost, not what was left of its budget.
    private decimal SalesRemainderFor(string costCode) =>
        (byCode.TryGetValue(costCode, out var row) ? RowSales(row) - RowNonWo(row) : 0m)
        - (WorkOrderCommittedByCode.TryGetValue(costCode, out var committed) ? committed : 0m)
        + PackagedWoFor(costCode);

    // Locked centres: sales value less forecast cost — realised profit / loss.
    private decimal ProfitLossFor(IReadOnlyList<string> costCodes) =>
        costCodes.Where(IsCentreFinalised).Sum(SalesRemainderFor);

    private static string LockTitle(ReportLine line, bool allLocked, bool anyLocked) =>
        allLocked
            ? "Unlock — the target-cost remainder returns to drawdown as available funds"
            : anyLocked
                ? "Some centres in this roll-up are already locked — lock the rest so the whole line reads as profit / loss"
                : line.IsGroup
                    ? "Lock every centre in this roll-up — no more spend expected; sales less forecast cost becomes profit / loss"
                    : "Lock this cost centre — no more spend expected; sales less forecast cost becomes profit / loss";

    private bool IsSaving(string key) => string.Equals(savingKey, key, StringComparison.OrdinalIgnoreCase);

    private async Task HandleCostCompletionChangedAsync(ReportLine line, ChangeEventArgs args)
    {
        if (!decimal.TryParse(Convert.ToString(args.Value),
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var percent)) { StateHasChanged(); return; }
        percent = Math.Clamp(percent, 0m, 100m);
        if (percent == CostCompletionFor(line.CostCodes)) return;
        savingKey = line.Key;
        await OnCostCompletionChanged.InvokeAsync((line.CostCodes, percent));
    }

    // "Zero" means every figure the row *displays* is zero — so this tests the same
    // netted accessors the cells render, not the raw summary fields. With "Hide scope
    // in packages" on, a centre whose money sits entirely inside packages nets to zero
    // and hides here; its position still shows on the package rows. Locked centres and
    // centres carrying a cost-completion percent stay visible (both are shown on the
    // row even when the money columns read £0).
    private bool HasAnyValue(IReadOnlyList<string> costCodes) =>
        costCodes.Any(entryCode =>
            (byCode.TryGetValue(entryCode, out var row) &&
             (RowSales(row) != 0m || RowClaimed(row) != 0m || RowTarget(row) != 0m ||
              RowActual(row) != 0m || RowNonWo(row) != 0m ||
              row.CostCompletionPercent != 0m || row.IsFinalised)) ||
            (WorkOrderCommittedByCode.TryGetValue(entryCode, out var committed) ? committed : 0m)
                - PackagedWoFor(entryCode) != 0m);

    // Every report line, before the hide-zero-rows and search narrowing — what the
    // export menu's "Include all rows" row asks for.
    private IEnumerable<ReportLine> AllReportLines
    {
        get
        {
            var grouped = GroupedCodes;

            var individuals = CostCenters
                .Where(centre => !grouped.Contains(centre.Code))
                .Select(centre => new ReportLine(centre.Code, centre.Code, centre.Name, InMaster: true, IsGroup: false, new[] { centre.Code }))
                .Concat(UnmatchedCodes
                    .Where(unmatched => !grouped.Contains(unmatched))
                    .Select(unmatched => new ReportLine(unmatched, unmatched, "", InMaster: false, IsGroup: false, new[] { unmatched })));

            var rollUps = Groups.Select(group => new ReportLine(
                group.CostCentreGroupId, "", group.Name, InMaster: true, IsGroup: true, group.CostCodes));

            return ApplySort(individuals.Concat(rollUps));
        }
    }
}
