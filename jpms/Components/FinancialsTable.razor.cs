using Jewel.JPMS.Services.Excel;
using static Jewel.JPMS.MoneyFormats;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Components;

public partial class FinancialsTable
{
    [Parameter, EditorRequired] public IReadOnlyList<CostCenter> CostCenters { get; set; } = Array.Empty<CostCenter>();
    [Parameter] public IReadOnlyList<ProjectFinancialSummaryRow> SummaryRows { get; set; } = Array.Empty<ProjectFinancialSummaryRow>();

    /// <summary>Named roll-ups: each renders as one aggregated line instead of its members.</summary>
    [Parameter] public IReadOnlyList<CostCentreGroup> Groups { get; set; } = Array.Empty<CostCentreGroup>();

    /// <summary>Committed work-order value (order line totals) per cost code, from the
    /// project's work orders. Rendered as the WO Committed column.</summary>
    [Parameter] public IReadOnlyDictionary<string, decimal> WorkOrderCommittedByCode { get; set; } =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Reconciliation package rows, rendered as first-class lines (and counted
    /// in the total) while "Hide scope in packages" is on — the scope netted off the
    /// centres sits here, so the table still covers the whole project.</summary>
    [Parameter] public IReadOnlyList<PackageReconciliationRow> Packages { get; set; } = Array.Empty<PackageReconciliationRow>();

    /// <summary>Raised with a heading and the line's cost codes when a Contract Sales Value figure
    /// is clicked (one code for an individual row, all members for a roll-up).</summary>
    [Parameter] public EventCallback<(string Heading, IReadOnlyList<string> CostCodes)> OnContractSalesSelected { get; set; }

    /// <summary>Raised with a heading and the line's cost codes when a Work Orders figure is clicked.</summary>
    [Parameter] public EventCallback<(string Heading, IReadOnlyList<string> CostCodes)> OnWorkOrderCommittedSelected { get; set; }

    /// <summary>Raised with a heading and the line's cost codes when an Actual Cost of Sales figure is clicked.</summary>
    [Parameter] public EventCallback<(string Heading, IReadOnlyList<string> CostCodes)> OnCostOfSalesSelected { get; set; }

    /// <summary>Raised with a heading and the line's cost codes when its Report button is
    /// clicked — the parent opens the cost-centre reconciliation modal.</summary>
    [Parameter] public EventCallback<(string Heading, IReadOnlyList<string> CostCodes)> OnReportSelected { get; set; }

    /// <summary>Raised when a line's lock button is clicked: the line's cost codes and the state
    /// to apply to all of them (lock everything unless every member is already locked).</summary>
    [Parameter] public EventCallback<(IReadOnlyList<string> CostCodes, bool Finalise)> OnFinalisationToggled { get; set; }

    /// <summary>Raised when the user asks to roll the selection up: the individually
    /// selected cost codes plus any selected roll-ups (which will be merged into the new
    /// group). The parent collects a name and creates the group.</summary>
    [Parameter] public EventCallback<(IReadOnlyList<string> CostCodes, IReadOnlyList<string> GroupIds)> OnGroupRequested { get; set; }

    /// <summary>Raised with the group id when a roll-up's Ungroup button is clicked.</summary>
    [Parameter] public EventCallback<string> OnUngroupRequested { get; set; }

    /// <summary>Raised when a line's Cost % Complete is edited; the parent persists the value
    /// to every listed cost code (one for an individual row, all members for a roll-up) and
    /// refreshes the summary. The line being saved renders its input disabled until then.</summary>
    [Parameter] public EventCallback<(IReadOnlyList<string> CostCodes, decimal Percent)> OnCostCompletionChanged { get; set; }

    // Report shape (agreed 2026-07): sales side then cost side, per report line. A line is
    // either one cost centre or a named roll-up of several (aggregated by simple sum; the
    // two percentages are value-weighted).
    //   Contract Sales Value = the valuation report's counting lines (contract works,
    //                          provisional sums, contingency and variations; declined/TBC excluded)
    //   % Complete           = sales-side completion from the latest claim — edited on the valuation
    //   Claim Value          = the latest claim's cumulative claimed £
    //   Target Cost Value    = Contract Sales Value ÷ (1 + FinancialSummaryAssumptions.MarkupPercent)
    //                          — what the line should cost us; target cost + markup = sales value
    //   Work Orders          = committed work-order line totals (click for the line's work orders)
    //   Non-WO Cost of Sales = the share of actual purchase spend not linked to any work order
    //   Committed Cost of Sales = Work Orders + Non-WO Cost of Sales — what is already
    //                          committed against the line (finance meeting 2026-08-03; this is
    //                          the column that used to be called Forecasted Cost of Sales)
    //   Drawdown             = Target Cost Value − Committed Cost of Sales, POSITIVE ONLY,
    //                          summed per centre — funds still available on the line. Locked
    //                          (finalised) centres show — here and report in Profit / Loss instead
    //   Overspend            = the same remainder where NEGATIVE, summed per centre — committed
    //                          cost already past the line's target. Split per centre, not on the
    //                          netted line total, so one underspent centre in a roll-up cannot
    //                          hide another's overspend. The Cashflow tab spends Drawdown only
    //                          and shows Overspend as the available buy back — never netted
    //   Forecasted Cost of Sales = Committed Cost of Sales + Drawdown — the cost side's
    //                          subtotal: an underspent line is forecast to spend its full
    //                          target, an overspent one what is already committed
    //   Profit / Loss        = Contract Sales Value − Forecasted Cost of Sales for locked
    //                          centres — the realised profit on the line (what it sold for
    //                          less what it cost), banked by finalising
    //   Cost % Complete      = cost-side completion, edited inline here
    //   Actual Cost of Sales = reference column, kept last: allocated Xero purchase spend, net
    //                          of credit notes — click through to the invoice modal to allocate
    //                          actual costs to work orders. Not part of the drawdown arithmetic.

    /// <summary>One rendered row: an individual cost centre or a named roll-up.</summary>
    private sealed record ReportLine(
        string Key,                        // cost code, or group id for roll-ups
        string Code,
        string Name,
        bool InMaster,
        bool IsGroup,
        IReadOnlyList<string> CostCodes);

    private Dictionary<string, ProjectFinancialSummaryRow> byCode = new(StringComparer.OrdinalIgnoreCase);

    // Hide cost centres with no figures in any column; on by default. Roll-ups are always
    // shown — they exist because someone deliberately created them.
    private bool hideZeroRows = true;

    // Net reconciliation-package scope out of every figure; on by default. The packaged
    // money's position lives on the package rows below, so counting it here too would
    // double-count it — e.g. a packaged purchase invoice still showing in the centre's
    // Actual Cost of Sales.
    private bool hidePackagedScope = true;

    // Filters rows by cost centre name or code; roll-ups match on their name or any
    // member code. Selected rows stay visible so a selection can't silently vanish
    // mid-roll-up. Totals stay project-wide, matching the hide-zero behaviour.
    private string search = string.Empty;

    private bool MatchesSearch(ReportLine line)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        if (selectedCodes.Contains(line.Key) || selectedGroupIds.Contains(line.Key)) return true;
        var term = search.Trim();
        return line.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
               || line.CostCodes.Any(entryCode => entryCode.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    // Codes and existing roll-ups ticked for the next roll-up. Selected roll-ups are
    // merged into the new group rather than nested.
    private readonly HashSet<string> selectedCodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> selectedGroupIds = new(StringComparer.OrdinalIgnoreCase);

    // The line whose Cost % Complete edit is currently in flight, if any.
    private string? savingKey;

    // Header sorting: null = the default alphabetical-by-name order. Text columns start
    // ascending; value columns start descending (biggest first — what the numbers are
    // usually sorted for). Clicking the active column flips the direction.
    private string? sortColumn;
    private bool sortDescending;

    private static readonly HashSet<string> TextSortColumns = new(StringComparer.Ordinal) { "code", "name" };

    private void SortBy(string column)
    {
        if (sortColumn == column)
        {
            sortDescending = !sortDescending;
            return;
        }
        sortColumn = column;
        sortDescending = !TextSortColumns.Contains(column);
    }


    private decimal SortValue(ReportLine line, string column) => column switch
    {
        "sales" => ContractSalesFor(line.CostCodes),
        "complete" => SalesCompletionFor(line.CostCodes),
        "claim" => ClaimValueFor(line.CostCodes),
        "target" => TargetCostFor(line.CostCodes),
        "wo" => WoCommittedFor(line.CostCodes),
        "committed" => CommittedFor(line.CostCodes),
        "forecast" => ForecastFor(line.CostCodes),
        "acos" => CostOfSalesFor(line.CostCodes),
        "nonwo" => NonWoCostOfSalesFor(line.CostCodes),
        "drawdown" => DrawdownFor(line.CostCodes),
        "overspend" => OverspendFor(line.CostCodes),
        "pl" => ProfitLossFor(line.CostCodes),
        "costpct" => CostCompletionFor(line.CostCodes),
        _ => 0m
    };

    private IEnumerable<ReportLine> ApplySort(IEnumerable<ReportLine> lines) => sortColumn switch
    {
        null => lines.OrderBy(line => line.Name, StringComparer.OrdinalIgnoreCase),
        "code" => sortDescending
            ? lines.OrderByDescending(line => line.Code, StringComparer.OrdinalIgnoreCase)
            : lines.OrderBy(line => line.Code, StringComparer.OrdinalIgnoreCase),
        "name" => sortDescending
            ? lines.OrderByDescending(line => line.Name, StringComparer.OrdinalIgnoreCase)
            : lines.OrderBy(line => line.Name, StringComparer.OrdinalIgnoreCase),
        _ => (sortDescending
                ? lines.OrderByDescending(line => SortValue(line, sortColumn))
                : lines.OrderBy(line => SortValue(line, sortColumn)))
            .ThenBy(line => line.Name, StringComparer.OrdinalIgnoreCase)
    };

    protected override void OnParametersSet()
    {
        byCode = SummaryRows.ToDictionary(row => row.CostCode, StringComparer.OrdinalIgnoreCase);
        // New rows have arrived (or the parent re-rendered after the save): re-enable the input.
        savingKey = null;
        // Codes that have just been rolled up can no longer be individually selected,
        // and merged-away roll-ups can no longer be selected either.
        selectedCodes.RemoveWhere(selected => GroupedCodes.Contains(selected));
        selectedGroupIds.RemoveWhere(selected =>
            !Groups.Any(group => string.Equals(group.CostCentreGroupId, selected, StringComparison.OrdinalIgnoreCase)));
    }

    private HashSet<string> GroupedCodes =>
        Groups.SelectMany(group => group.CostCodes).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private int SelectionCount => selectedCodes.Count + selectedGroupIds.Count;

    private void ToggleSelection(string costCode)
    {
        if (!selectedCodes.Remove(costCode)) selectedCodes.Add(costCode);
    }

    private void ToggleGroupSelection(string groupId)
    {
        if (!selectedGroupIds.Remove(groupId)) selectedGroupIds.Add(groupId);
    }

    private void ClearSelection()
    {
        selectedCodes.Clear();
        selectedGroupIds.Clear();
    }

    private Task RequestGroup() =>
        OnGroupRequested.InvokeAsync((
            (IReadOnlyList<string>)selectedCodes.OrderBy(selected => selected, StringComparer.OrdinalIgnoreCase).ToList(),
            (IReadOnlyList<string>)selectedGroupIds.OrderBy(selected => selected, StringComparer.OrdinalIgnoreCase).ToList()));

    private static string Heading(ReportLine line) =>
        line.IsGroup ? line.Name : $"{line.Code} — {(line.InMaster ? line.Name : "(not in cost-code master)")}";

    private decimal SumFor(IReadOnlyList<string> costCodes, Func<ProjectFinancialSummaryRow, decimal> value) =>
        costCodes.Sum(entryCode => byCode.TryGetValue(entryCode, out var row) ? value(row) : 0m);

    // With "Hide scope in packages" on (the default), every figure nets off what
    // reconciliation packages already account for — the packaged money's position lives
    // on the package row instead, so this table shows only unreconciled scope and its
    // drawdown stops being polluted by mismatches that a package has already resolved.
    private decimal RowSales(ProjectFinancialSummaryRow row) =>
        row.BudgetedSales - (hidePackagedScope ? row.PackagedSales : 0m);

    private decimal RowClaimed(ProjectFinancialSummaryRow row) =>
        row.ClaimedToDate - (hidePackagedScope ? row.PackagedClaimed : 0m);


    private IEnumerable<ReportLine> VisibleLines =>
        AllReportLines
            .Where(line => line.IsGroup || !hideZeroRows || HasAnyValue(line.CostCodes) || selectedCodes.Contains(line.Key))
            .Where(MatchesSearch);

    // Packages count in the table (rows + totals) only while the netting toggle is on —
    // with gross figures on the centres, package rows would double-count their scope.
    private IEnumerable<PackageReconciliationRow> IncludedPackages =>
        hidePackagedScope ? Packages : Array.Empty<PackageReconciliationRow>();

    // Rendered package rows follow the search like centre rows do; totals stay
    // project-wide, matching the hide-zero and search behaviour.
    private IEnumerable<PackageReconciliationRow> VisiblePackages =>
        IncludedPackages.Where(package =>
            string.IsNullOrWhiteSpace(search)
            || package.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));

    private IEnumerable<string> UnmatchedCodes =>
        SummaryRows.Select(row => row.CostCode)
            .Union(WorkOrderCommittedByCode.Keys, StringComparer.OrdinalIgnoreCase)
            .Where(rowCode => !CostCenters.Any(centre => string.Equals(centre.Code, rowCode, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(rowCode => rowCode, StringComparer.OrdinalIgnoreCase);

    // Totals go through the same per-row accessors as the lines, so they respect the
    // hide-packaged-scope toggle and always equal the sum of what's on screen. Package
    // rows are added back in, so the total covers the whole project either way — with
    // the toggle on, the packaged scope simply arrives via the package rows instead of
    // the centres.
    private decimal TotalContractSales => SummaryRows.Sum(RowSales) + IncludedPackages.Sum(package => package.SalesValue);
    private decimal TotalClaimValue => SummaryRows.Sum(RowClaimed) + IncludedPackages.Sum(package => package.ClaimedToDate);
    private decimal TotalTargetCost => SummaryRows.Sum(RowTarget) + IncludedPackages.Sum(package => package.TargetCost);
    private decimal TotalWoCommitted =>
        WorkOrderCommittedByCode.Sum(entry => entry.Value - PackagedWoFor(entry.Key))
        + IncludedPackages.Sum(package => package.WoCommitted);
    private decimal TotalCostOfSales => SummaryRows.Sum(RowActual) + IncludedPackages.Sum(package => package.InvoicedToDate);
    private decimal TotalNonWoCostOfSales => SummaryRows.Sum(RowNonWo);

    // Work orders + non-WO spend across the centres, plus each unlocked package's
    // committed-or-direct figure (its target less its drawdown). Locked packages show —
    // on the line (figures banked at lock), so they add nothing here either.
    private decimal TotalCommittedCostOfSales =>
        WorkOrderCommittedByCode.Sum(entry => entry.Value - PackagedWoFor(entry.Key))
        + SummaryRows.Sum(RowNonWo)
        + IncludedPackages.Where(package => !package.IsLocked).Sum(package => package.TargetCost - package.Drawdown);

    // Committed plus the drawdown still to be spent — matches the per-line subtotal column.
    private decimal TotalForecastCostOfSales => TotalCommittedCostOfSales + TotalDrawdown;

    // Every code with any figure — summary rows plus work-order-only codes.
    private IEnumerable<string> AllCodes =>
        byCode.Keys.Union(WorkOrderCommittedByCode.Keys, StringComparer.OrdinalIgnoreCase);

    // Delegated to the shared calculator so the Cashflow tab's drawdown and buy-back rows
    // always agree with this pair. The gross (packaged-scope-off) view keeps its per-centre sum — no package
    // rows in play then. Split per centre either way, matching the line cells.
    private DrawdownSplit TotalDrawdownSplit =>
        hidePackagedScope
            ? ProjectDrawdown.SplitForProject(SummaryRows, WorkOrderCommittedByCode, Packages)
            : new DrawdownSplit(
                AllCodes.Where(entryCode => !IsCentreFinalised(entryCode)).Sum(entryCode => Math.Max(0m, RemainderFor(entryCode))),
                AllCodes.Where(entryCode => !IsCentreFinalised(entryCode)).Sum(entryCode => Math.Min(0m, RemainderFor(entryCode))));

    private decimal TotalDrawdown => TotalDrawdownSplit.Drawdown;

    private decimal TotalOverspend => TotalDrawdownSplit.Overspend;
    private decimal TotalProfitLoss =>
        AllCodes.Where(IsCentreFinalised).Sum(SalesRemainderFor)
        + IncludedPackages.Where(package => package.IsLocked).Sum(package => package.ProfitLoss);

    // Matching the per-line calculation: claimed £ over contract £ (both netted of
    // packaged scope when the toggle is on).
    private decimal WeightedSalesCompletion =>
        TotalContractSales == 0m
            ? 0m
            : TotalClaimValue / TotalContractSales * 100m;

    // Weighted by target cost, the base the cost side draws down against. Weighted over
    // the centre rows only — packages carry no cost-side completion — so the weights
    // match the base they divide by.
    private decimal WeightedCostCompletion
    {
        get
        {
            var rowsTarget = SummaryRows.Sum(RowTarget);
            return rowsTarget == 0m
                ? 0m
                : SummaryRows.Sum(row => row.CostCompletionPercent * RowTarget(row)) / rowsTarget;
        }
    }

    private string MoneyClass(decimal value) => value == 0m ? "text-content-muted" : "text-content";

    private static string InputValue(decimal value) =>
        value.ToString(value == Math.Truncate(value) ? "0" : "0.#");


    private static string Percent(decimal value) =>
        $"{value.ToString(value == Math.Truncate(value) ? "0" : "0.#")}%";

}
