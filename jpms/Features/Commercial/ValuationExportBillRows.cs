using Jewel.JPMS.Services.Excel;
using static Jewel.JPMS.Features.Commercial.ValuationExportStyles;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>The rows of one bill section on a statement tab: column headings, one row per line, the section total.</summary>
internal static class ValuationExportBillRows
{
    private const decimal WholePercent = 100m;

    public static void AddColumnHeadings(ExcelSheet sheet, bool hasClientReference)
    {
        var cells = new List<object?>
        {
            new ExcelStyledCell("Code", ColHead),
            new ExcelStyledCell("Description", ColHead),
            new ExcelStyledCell("Unit", ColHead),
            new ExcelStyledCell("Qty", ColHeadRight),
            new ExcelStyledCell("Rate £", ColHeadRight),
            new ExcelStyledCell("Amount £", ColHeadRight),
            new ExcelStyledCell("% Complete", ColHeadRight),
            new ExcelStyledCell("Previous £", ColHeadRight),
            new ExcelStyledCell("This period £", ColHeadRight),
            new ExcelStyledCell("Claimed £", ColHeadRight),
        };
        if (hasClientReference) cells.Insert(1, new ExcelStyledCell("Client ref", ColHead));
        sheet.AddRow(cells.ToArray());
    }

    public static void AddLine(ExcelSheet sheet, ValuationExportLine line, bool hasClientReference)
    {
        var moved = line.MovedThisPeriod;
        var cells = new List<object?>
        {
            new ExcelStyledCell(line.Code, Code(moved)),
            new ExcelStyledCell(DescriptionFor(line), Desc(moved)),
            new ExcelStyledCell(line.Unit, Text(moved)),
            new ExcelStyledCell(line.Quantity, Num(moved)),
            new ExcelStyledCell(line.Rate, Num(moved)),
            new ExcelStyledCell(line.LineAmount, Money(moved, negative: line.LineAmount < 0m)),
            ClaimCell(line, line.PercentComplete / WholePercent, Pct(moved)),
            ClaimCell(line, line.PreviousClaimed, Money(moved)),
            ClaimCell(line, line.ThisPeriod, Money(moved, negative: line.ThisPeriod < 0m, strong: moved)),
            ClaimCell(line, line.CumulativeClaimed, Money(moved)),
        };
        if (hasClientReference) cells.Insert(1, new ExcelStyledCell(line.ClientReference, Text(moved)));
        sheet.AddRow(cells.ToArray());
    }

    // The description carries the comment beneath the title, and flags a line that is
    // recorded but never priced into a total.
    private static string DescriptionFor(ValuationExportLine line)
    {
        var description = line.Title;
        if (!string.IsNullOrWhiteSpace(line.Comments)) description += "\n" + line.Comments;
        if (!line.CountsTowardTotals) description += $"\n[{line.LineTypeLabel} — not priced into totals]";
        return description;
    }

    // Claim figures only exist on a line priced into the totals; the rest show an empty cell.
    private static ExcelStyledCell ClaimCell(ValuationExportLine line, decimal value, ExcelCellStyle style) =>
        line.CountsTowardTotals
            ? new ExcelStyledCell(value, style)
            : new ExcelStyledCell(null, Text(line.MovedThisPeriod));

    // Money columns are addressed from the END of the row, so the same code serves the grid
    // with and without the client-reference column.
    public static void AddSectionTotal(ExcelSheet sheet, string sectionTitle, IReadOnlyList<ValuationExportLine> lines)
    {
        var counting = lines.Where(line => line.CountsTowardTotals).ToList();
        var cells = ValuationExportStatementSheet.FilledCells(sheet, TotalFill);
        cells[^9] = new ExcelStyledCell($"{sectionTitle} total", TotalLabel); // the Description column in either grid
        cells[^5] = new ExcelStyledCell(counting.Sum(line => line.LineAmount), TotalMoney);
        cells[^3] = new ExcelStyledCell(counting.Sum(line => line.PreviousClaimed), TotalMoney);
        cells[^2] = new ExcelStyledCell(counting.Sum(line => line.ThisPeriod), TotalMoney);
        cells[^1] = new ExcelStyledCell(counting.Sum(line => line.CumulativeClaimed), TotalMoney);
        sheet.AddRow(cells);
    }
}
