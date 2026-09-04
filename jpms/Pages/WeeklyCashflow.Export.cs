using Jewel.JPMS.Contracts.WeeklyCashflow.Export;

namespace Jewel.JPMS.Pages;

public partial class WeeklyCashflow
{
    // The workbook is the screen on paper: the "Weekly plan" tab is the grid as rendered, one
    // line per supplier (a supplier group is one line, as here); "Detail" opens every line into
    // its bills; "Data" is the flat list for pivoting. WeeklyCashflowExport (contracts, unit-
    // tested) owns the layout — this page only hands over what it has already computed for the
    // render, so the tabs can never disagree with the grid.
    private ExcelWorkbook? BuildExportWorkbook(bool _)
    {
        if (!GridReady || !XeroReady) return null;
        // BuildView also refills excludedSeeds — the parked entries the tabs list uncounted.
        var view = BuildView();
        var plan = Plan.Current!;
        return WeeklyCashflowExport.Build(new WeeklyCashflowExportInput(
            view,
            plan.SupplierGroups,
            excludedSeeds.ToList(),
            plan.Exclusions,
            IsDirector,
            IsDirector && BankReady ? BankSnapshot!.TotalCash : null,
            PayablesSnapshot?.FetchedAtUtc?.ToLocalTime(),
            DateTimeOffset.Now));
    }
}
