using Jewel.JPMS.Contracts.Documents.Excel;

namespace Jewel.JPMS.Contracts.WeeklyCashflow.Export;

/// <summary>
/// The "Data" tab — the plan as a flat, filterable table for pivoting: one row per entry, in the
/// order the grid tabs read them, each naming the band and the line it sits on. The week is the
/// Monday as a real date, so a pivot can group by week or month; Later stays a word. Parked
/// entries are not rows here: they are uncounted, and a pivot must add up to the grid.
/// </summary>
internal static class WeeklyCashflowExportDataSheet
{
    public const string SheetName = "Data";
    private const string CashInDirection = "In";
    private const string CashOutDirection = "Out";
    private const string LaterWeek = "Later";
    private const string MovedMark = "moved";

    public static void Add(ExcelWorkbook workbook, WeeklyCashflowView view, IReadOnlyList<WeeklyCashflowExportBand> bands)
    {
        var sheet = workbook.AddSheet(SheetName,
            new ExcelColumn("Band"),
            new ExcelColumn("Direction"),
            new ExcelColumn("Line"),
            new ExcelColumn("Name"),
            new ExcelColumn("Detail"),
            new ExcelColumn("Due", ExcelFormat.Date),
            new ExcelColumn("Expected", ExcelFormat.Date),
            new ExcelColumn("Week commencing", ExcelFormat.Date),
            new ExcelColumn("Moved"),
            new ExcelColumn("Amount", ExcelFormat.Currency));

        foreach (var band in bands)
            foreach (var line in band.Lines)
                foreach (var entry in line.Entries)
                    sheet.AddRow(
                        band.Label,
                        band.IsCashIn ? CashInDirection : CashOutDirection,
                        line.Label,
                        entry.Label,
                        entry.Detail,
                        entry.NaturalDueOn?.UtcDateTime,
                        entry.ExpectedOn?.UtcDateTime,
                        WeekCommencing(view, entry),
                        entry.Moved ? MovedMark : "",
                        entry.Amount);
    }

    private static object WeekCommencing(WeeklyCashflowView view, WeeklyCashflowEntry entry) =>
        entry.WeekIndex == view.LaterIndex ? LaterWeek : (object)view.WeekStarts[entry.WeekIndex].UtcDateTime;
}
