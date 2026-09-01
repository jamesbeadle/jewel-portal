using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Components;

public partial class FinancialsTable
{
    // Exports exactly what's on screen: VisibleLines (respecting search, hide-zero-rows
    // and sort) plus VisiblePackages (respecting "hide scope in packages" and search),
    // then a total row matching the tfoot. Percentages are stored here as 0–100 — the
    // sheet wants fractions, so every percentage is divided by 100.
    // "Include all rows" lifts the search and hide-zero narrowing (the packaged-scope
    // netting toggle still applies — overriding it would double-count package scope);
    // the total row is project-wide either way, so it then ties to the exported rows.
    private ExcelWorkbook? BuildExportWorkbook(bool includeAllRows)
    {
        var lines = (includeAllRows ? AllReportLines : VisibleLines).ToList();
        var packages = (includeAllRows ? IncludedPackages : VisiblePackages).ToList();
        if (lines.Count == 0 && packages.Count == 0) return null;

        var workbook = new ExcelWorkbook();
        var sheet = workbook.AddSheet("Financial report",
            new ExcelColumn("Code"),
            new ExcelColumn("Cost centre"),
            new ExcelColumn("Contract sales value", ExcelFormat.Currency),
            new ExcelColumn("% complete", ExcelFormat.Percent),
            new ExcelColumn("Claim value", ExcelFormat.Currency),
            new ExcelColumn("Target cost value", ExcelFormat.Currency),
            new ExcelColumn("Work orders", ExcelFormat.Currency),
            new ExcelColumn("Non-WO cost of sales", ExcelFormat.Currency),
            new ExcelColumn("Committed cost of sales", ExcelFormat.Currency),
            new ExcelColumn("Drawdown", ExcelFormat.Currency),
            new ExcelColumn("Overspend", ExcelFormat.Currency),
            new ExcelColumn("Forecasted cost of sales", ExcelFormat.Currency),
            new ExcelColumn("Profit / loss", ExcelFormat.Currency),
            new ExcelColumn("Cost % complete", ExcelFormat.Percent),
            new ExcelColumn("Actual cost of sales", ExcelFormat.Currency));

        foreach (var line in lines)
        {
            var allLocked = AllFinalised(line.CostCodes);
            var anyLocked = AnyFinalised(line.CostCodes);
            sheet.AddRow(
                line.IsGroup ? $"{line.CostCodes.Count} centres" : line.Code,
                line.IsGroup ? line.Name : (line.InMaster ? line.Name : "(not in cost-code master)"),
                ContractSalesFor(line.CostCodes),
                SalesCompletionFor(line.CostCodes) / 100m,
                ClaimValueFor(line.CostCodes),
                TargetCostFor(line.CostCodes),
                WoCommittedFor(line.CostCodes),
                NonWoCostOfSalesFor(line.CostCodes),
                CommittedFor(line.CostCodes),
                allLocked ? (decimal?)null : DrawdownFor(line.CostCodes),
                allLocked ? (decimal?)null : OverspendFor(line.CostCodes),
                ForecastFor(line.CostCodes),
                anyLocked ? (decimal?)ProfitLossFor(line.CostCodes) : null,
                CostCompletionFor(line.CostCodes) / 100m,
                CostOfSalesFor(line.CostCodes));
        }

        foreach (var package in packages)
        {
            sheet.AddRow(
                "PKG",
                package.Name,
                package.SalesValue,
                (package.SalesValue == 0m ? 0m : Math.Round(package.ClaimedToDate / package.SalesValue * 100m, 1)) / 100m,
                package.ClaimedToDate,
                package.TargetCost,
                package.WoCommitted,
                (decimal?)null,
                package.IsLocked ? (decimal?)null : package.TargetCost - package.Drawdown,
                package.IsLocked ? (decimal?)null : Math.Max(0m, package.Drawdown),
                package.IsLocked ? (decimal?)null : Math.Min(0m, package.Drawdown),
                package.IsLocked ? (decimal?)null : package.TargetCost - package.Drawdown + Math.Max(0m, package.Drawdown),
                package.IsLocked ? (decimal?)package.ProfitLoss : null,
                (decimal?)null,
                package.InvoicedToDate);
        }

        sheet.AddRow(
            null,
            "Total",
            TotalContractSales,
            WeightedSalesCompletion / 100m,
            TotalClaimValue,
            TotalTargetCost,
            TotalWoCommitted,
            TotalNonWoCostOfSales,
            TotalCommittedCostOfSales,
            TotalDrawdown,
            TotalOverspend,
            TotalForecastCostOfSales,
            TotalProfitLoss,
            WeightedCostCompletion / 100m,
            TotalCostOfSales);

        return workbook;
    }

    // Pinned horizontal scrollbar (see the proxy markup after the table): the JS side
    // owns all the observation and syncing; the component just hands over the four
    // elements once and detaches them on dispose.
    private ElementReference scrollerRef;
    private ElementReference sentinelRef;
    private ElementReference proxyRef;
    private ElementReference spacerRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await Js.InvokeVoidAsync("jpmsStickyScroll.init", scrollerRef, sentinelRef, proxyRef, spacerRef);
    }

    public async ValueTask DisposeAsync()
    {
        // The JS runtime can already be gone when the page is torn down — nothing to
        // detach in that case.
        try { await Js.InvokeVoidAsync("jpmsStickyScroll.dispose", scrollerRef); }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
    }
}
