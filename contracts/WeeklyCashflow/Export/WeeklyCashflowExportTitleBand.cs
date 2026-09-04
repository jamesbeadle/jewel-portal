using System.Globalization;
using Jewel.JPMS.Contracts.Documents.Excel;
using static Jewel.JPMS.Contracts.WeeklyCashflow.Export.WeeklyCashflowExportStyles;

namespace Jewel.JPMS.Contracts.WeeklyCashflow.Export;

/// <summary>The navy title band at the top of each grid tab — what this is, which weeks it covers,
/// when it was exported and when Xero was read — with the legend beneath it. Everything above the
/// heading row ends up frozen, so the band stays four rows: no spacer of its own.</summary>
internal static class WeeklyCashflowExportTitleBand
{
    public const string Title = "WEEKLY CASHFLOW";
    public const string Company = "Jewel Bespoke Build";
    private const string LongDateFormat = "d MMM yyyy";
    private const string StampFormat = "d MMM yyyy HH:mm";
    // The right-hand block of the band spans the last four columns, as on the valuation tabs.
    private const int RightBlockColumns = 4;

    private const string LegendDirection =
        "Cash in adds, cash out subtracts. Each column is the week starting on the Monday shown; the first week is the current one and carries everything overdue. Later is due beyond the horizon.";
    private const string LegendMoves =
        "A shaded amount was moved to that week on the portal — its due (or Xero expected) week is on the Detail tab. Every figure is an outstanding Xero document or an item added on the page; moving changes when, never how much.";

    public static void Add(WeeklyCashflowExportGrid grid, WeeklyCashflowExportInput input)
    {
        AddSplitBandRow(grid, new ExcelStyledCell(Title, BandTitle), new ExcelStyledCell(HorizonLabel(input.View), BandGoldRight));
        AddSplitBandRow(grid, new ExcelStyledCell(Company, BandGold), new ExcelStyledCell(StampLabel(input), BandTextRight));
        grid.AddMergedRow(new ExcelStyledCell(LegendDirection, Legend));
        grid.AddMergedRow(new ExcelStyledCell(LegendMoves, Legend));
    }

    public static string HorizonLabel(WeeklyCashflowView view) =>
        $"{view.WeekStarts.Count} WEEKS FROM W/C {LongDate(view.WeekStarts[0]).ToUpperInvariant()}";

    public static string StampLabel(WeeklyCashflowExportInput input)
    {
        var exported = $"Exported {Stamp(input.ExportedAt)}";
        return input.XeroReadAt is { } readAt ? $"{exported} · Xero read {Stamp(readAt)}" : exported;
    }

    private static string LongDate(DateTimeOffset date) => date.UtcDateTime.ToString(LongDateFormat, CultureInfo.InvariantCulture);

    private static string Stamp(DateTimeOffset moment) => moment.ToString(StampFormat, CultureInfo.InvariantCulture);

    // Two navy blocks across the width, each merged into one cell.
    private static void AddSplitBandRow(WeeklyCashflowExportGrid grid, ExcelStyledCell left, ExcelStyledCell right)
    {
        var count = grid.ColumnCount;
        var rightStart = Math.Max(count - RightBlockColumns, 1);
        var cells = grid.Cells(Band);
        cells[0] = left;
        cells[rightStart] = right;
        grid.AddRow(cells);
        var row = grid.Sheet.Rows.Count;
        grid.Sheet.MergedRanges.Add($"A{row}:{WeeklyCashflowExportGrid.ColumnLetterAt(rightStart - 1)}{row}");
        grid.Sheet.MergedRanges.Add($"{WeeklyCashflowExportGrid.ColumnLetterAt(rightStart)}{row}:{WeeklyCashflowExportGrid.ColumnLetterAt(count - 1)}{row}");
    }
}
