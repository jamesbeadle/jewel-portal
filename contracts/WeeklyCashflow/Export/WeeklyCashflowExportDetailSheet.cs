using System.Globalization;
using Jewel.JPMS.Contracts.Documents.Excel;
using Jewel.JPMS.Models;
using static Jewel.JPMS.Contracts.WeeklyCashflow.Export.WeeklyCashflowExportStyles;

namespace Jewel.JPMS.Contracts.WeeklyCashflow.Export;

/// <summary>
/// The "Detail" tab — every line of the plan opened up, as the grid reads with everything
/// expanded: the band's totals, each line in bold, and beneath it every bill, invoice or
/// occurrence with its reference, due date and Xero expected/planned date, its amount in its
/// week. Parked entries close each band, small and uncounted, as they do on screen.
/// </summary>
internal static class WeeklyCashflowExportDetailSheet
{
    public const string SheetName = "Detail";
    private const string EntryHeading = "Supplier / entry";
    private const string ReferenceHeading = "Reference";
    private const string DueHeading = "Due";
    private const string ExpectedHeading = "Expected";
    private const double EntryColumnWidth = 40;
    private const double ReferenceColumnWidth = 26;
    private const double DateColumnWidth = 11;
    private const string MemberIndent = "    ";
    private const string MoneyTextFormat = "C";
    private const string MoneyCulture = "en-GB";
    private const int ReferenceColumn = 1;
    private const int DueColumn = 2;
    private const int ExpectedColumn = 3;

    public static void Add(ExcelWorkbook workbook, WeeklyCashflowExportInput input, IReadOnlyList<WeeklyCashflowExportBand> bands)
    {
        var leading = new[]
        {
            new ExcelColumn(EntryHeading, Width: EntryColumnWidth),
            new ExcelColumn(ReferenceHeading, Width: ReferenceColumnWidth),
            new ExcelColumn(DueHeading, ExcelFormat.Date, DateColumnWidth),
            new ExcelColumn(ExpectedHeading, ExcelFormat.Date, DateColumnWidth)
        };
        var grid = new WeeklyCashflowExportGrid(workbook, SheetName, input.View, leading);
        WeeklyCashflowExportTitleBand.Add(grid, input);
        grid.AddBlankRow();
        grid.AddHeadingRow(EntryHeading, ReferenceHeading, DueHeading, ExpectedHeading);

        grid.AddSectionHeading(WeeklyCashflowExportPlanSheet.CashInHeading);
        foreach (var band in bands.Where(candidate => candidate.IsCashIn)) AddBand(grid, band, input);
        grid.AddSectionHeading(WeeklyCashflowExportPlanSheet.CashOutHeading);
        foreach (var band in bands.Where(candidate => !candidate.IsCashIn)) AddBand(grid, band, input);

        grid.AddNetRow();
        if (input.View.Closing is { } closing) grid.AddClosingRow(closing);
    }

    private static void AddBand(WeeklyCashflowExportGrid grid, WeeklyCashflowExportBand band, WeeklyCashflowExportInput input)
    {
        grid.AddBandRow(band);
        foreach (var line in band.Lines)
        {
            grid.AddLineRow(line, LineLabelBold);
            foreach (var entry in line.Entries) AddEntryRow(grid, entry);
        }
        foreach (var seed in input.ExcludedSeeds.Where(candidate => candidate.Band == band.Band))
            AddExcludedRow(grid, seed, ExcludedBy(input.Exclusions, seed));
    }

    private static void AddEntryRow(WeeklyCashflowExportGrid grid, WeeklyCashflowEntry entry)
    {
        var cells = grid.LineCells(MemberIndent + entry.Label, Line);
        cells[ReferenceColumn] = new ExcelStyledCell(entry.Detail, LineText);
        cells[DueColumn] = new ExcelStyledCell(entry.NaturalDueOn?.UtcDateTime, LineDate);
        cells[ExpectedColumn] = new ExcelStyledCell(entry.ExpectedOn?.UtcDateTime, LineDate);
        grid.PlaceAmount(cells, entry.WeekIndex, entry.Amount, entry.Moved);
        grid.PlaceTotal(cells, entry.Amount);
        grid.AddRow(cells);
    }

    // A parked entry is in no week and in no total — its amount travels in the text, so a sum down
    // the Total column still equals the band row above it.
    private static void AddExcludedRow(WeeklyCashflowExportGrid grid, WeeklyCashflowSeed seed, string? excludedBy)
    {
        var cells = grid.LineCells(MemberIndent + seed.Label, Excluded);
        var amount = seed.Amount.ToString(MoneyTextFormat, CultureInfo.GetCultureInfo(MoneyCulture));
        var who = excludedBy is null ? "" : $" · {excludedBy}";
        cells[ReferenceColumn] = new ExcelStyledCell($"{seed.Detail} · excluded — not counted ({amount}){who}", Excluded);
        cells[DueColumn] = new ExcelStyledCell(seed.DueOn?.UtcDateTime, LineDate);
        cells[ExpectedColumn] = new ExcelStyledCell(seed.ExpectedOn?.UtcDateTime, LineDate);
        grid.AddRow(cells);
    }

    private static string? ExcludedBy(IReadOnlyList<WeeklyCashflowExclusion> exclusions, WeeklyCashflowSeed seed) =>
        exclusions
            .FirstOrDefault(exclusion => exclusion.PlacementKey == seed.PlacementKey)
            ?.ExcludedByEmail;
}
