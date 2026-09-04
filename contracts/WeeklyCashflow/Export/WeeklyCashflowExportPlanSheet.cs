using System.Globalization;
using Jewel.JPMS.Contracts.Documents.Excel;
using static Jewel.JPMS.Contracts.WeeklyCashflow.Export.WeeklyCashflowExportStyles;

namespace Jewel.JPMS.Contracts.WeeklyCashflow.Export;

/// <summary>
/// The "Weekly plan" tab — the grid as the accountant reads it: the tiles, then Cash in and
/// Cash out with a totals row per band and one line per supplier, client or item beneath, then
/// Net movement and (directors) the closing bank balance. Parked entries are counted in a
/// note only; the Detail tab lists them.
/// </summary>
internal static class WeeklyCashflowExportPlanSheet
{
    public const string SheetName = "Weekly plan";
    public const string CashInHeading = "Cash in";
    public const string CashOutHeading = "Cash out";
    private const string EntryHeading = "Entry";
    private const double EntryColumnWidth = 40;
    private const string MoneyTextFormat = "C0";
    private const string MoneyCulture = "en-GB";

    public static void Add(ExcelWorkbook workbook, WeeklyCashflowExportInput input, IReadOnlyList<WeeklyCashflowExportBand> bands)
    {
        var grid = new WeeklyCashflowExportGrid(workbook, SheetName, input.View, new[] { new ExcelColumn(EntryHeading, Width: EntryColumnWidth) });
        WeeklyCashflowExportTitleBand.Add(grid, input);
        grid.AddBlankRow();
        WeeklyCashflowExportSummary.Add(grid, input);
        grid.AddHeadingRow(EntryHeading);

        grid.AddSectionHeading(CashInHeading);
        foreach (var band in bands.Where(candidate => candidate.IsCashIn)) AddBand(grid, band);
        grid.AddSectionHeading(CashOutHeading);
        foreach (var band in bands.Where(candidate => !candidate.IsCashIn)) AddBand(grid, band);

        grid.AddNetRow();
        if (input.View.Closing is { } closing) grid.AddClosingRow(closing);
        AddExcludedNote(grid, input.ExcludedSeeds);
    }

    private static void AddBand(WeeklyCashflowExportGrid grid, WeeklyCashflowExportBand band)
    {
        grid.AddBandRow(band);
        foreach (var line in band.Lines) grid.AddLineRow(line, Line);
    }

    // The money parked by exclusions is named, not hidden — the reader knows the Detail tab holds it.
    private static void AddExcludedNote(WeeklyCashflowExportGrid grid, IReadOnlyList<WeeklyCashflowSeed> excludedSeeds)
    {
        if (excludedSeeds.Count == 0) return;
        grid.AddBlankRow();
        var total = excludedSeeds.Sum(seed => seed.Amount);
        var noun = excludedSeeds.Count == 1 ? "entry" : "entries";
        var money = total.ToString(MoneyTextFormat, CultureInfo.GetCultureInfo(MoneyCulture));
        grid.AddMergedRow(new ExcelStyledCell(
            $"{excludedSeeds.Count} {noun} excluded on the portal ({money}) — parked and not counted above; listed on the Detail tab.",
            Legend));
    }
}
