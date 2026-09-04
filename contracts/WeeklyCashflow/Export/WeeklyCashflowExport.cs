using Jewel.JPMS.Contracts.Documents.Excel;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.WeeklyCashflow.Export;

/// <summary>
/// What the Weekly Cashflow page hands its export: the grid as rendered (<see cref="View"/>),
/// the plan's supplier groups so the tabs fold bills into the same lines as the screen, the
/// Xero seeds parked by an exclusion (with the exclusions themselves, for who parked them), and
/// the tile facts — the bank position is null for anyone who isn't a director or when Xero has
/// not answered, exactly as the tiles behave. Times are local, as the page shows them.
/// </summary>
public sealed record WeeklyCashflowExportInput(
    WeeklyCashflowView View,
    IReadOnlyList<WeeklyCashflowSupplierGroup> SupplierGroups,
    IReadOnlyList<WeeklyCashflowSeed> ExcludedSeeds,
    IReadOnlyList<WeeklyCashflowExclusion> Exclusions,
    bool IsDirector,
    decimal? CashInBank,
    DateTimeOffset? XeroReadAt,
    DateTimeOffset ExportedAt);

/// <summary>
/// The Weekly Cashflow workbook — the screen, on paper. Three tabs from one computation:
/// "Weekly plan" is the grid as the accountant reads it, one line per supplier (a supplier
/// group is one line, as on screen) with a column per week; "Detail" opens every line into
/// the bills and invoices behind it, in the same grid; "Data" is the flat list for filtering
/// and pivoting. Pure — no HTTP, no browser — so the layout is unit-tested
/// (WeeklyCashflowExportTests) and can never disagree with WeeklyCashflowMaths.
/// </summary>
public static class WeeklyCashflowExport
{
    public static ExcelWorkbook Build(WeeklyCashflowExportInput input)
    {
        var bands = WeeklyCashflowExportBands.For(input.View, input.SupplierGroups);
        var workbook = new ExcelWorkbook();
        WeeklyCashflowExportPlanSheet.Add(workbook, input, bands);
        WeeklyCashflowExportDetailSheet.Add(workbook, input, bands);
        WeeklyCashflowExportDataSheet.Add(workbook, input.View, bands);
        return workbook;
    }
}
