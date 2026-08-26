using Jewel.JPMS.Services.Excel;
using static Jewel.JPMS.Features.Commercial.ValuationExportStyles;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>The rows of one bill section on a statement tab: column headings, one row per line, the section total.</summary>
internal static class ValuationExportBillRows
{
    private const decimal WholePercent = 100m;

    public static void AddColumnHeadings(ExcelSheet sheet) =>
        sheet.AddRow(
            new ExcelStyledCell("Code", ColHead),
            new ExcelStyledCell("Description", ColHead),
            new ExcelStyledCell("Unit", ColHead),
            new ExcelStyledCell("Qty", ColHeadRight),
            new ExcelStyledCell("Rate £", ColHeadRight),
            new ExcelStyledCell("Amount £", ColHeadRight),
            new ExcelStyledCell("% Complete", ColHeadRight),
            new ExcelStyledCell("Previous £", ColHeadRight),
            new ExcelStyledCell("This period £", ColHeadRight),
            new ExcelStyledCell("Claimed £", ColHeadRight));

    public static void AddLine(ExcelSheet sheet, ValuationExportLine line)
    {
        var moved = line.MovedThisPeriod;
        sheet.AddRow(
            new ExcelStyledCell(line.Code, Code(moved)),
            new ExcelStyledCell(DescriptionFor(line), Desc(moved)),
            new ExcelStyledCell(line.Unit, Text(moved)),
            new ExcelStyledCell(line.Quantity, Num(moved)),
            new ExcelStyledCell(line.Rate, Num(moved)),
            new ExcelStyledCell(line.LineAmount, Money(moved, negative: line.LineAmount < 0m)),
            ClaimCell(line, line.PercentComplete / WholePercent, Pct(moved)),
            ClaimCell(line, line.PreviousClaimed, Money(moved)),
            ClaimCell(line, line.ThisPeriod, Money(moved, negative: line.ThisPeriod < 0m, strong: moved)),
            ClaimCell(line, line.CumulativeClaimed, Money(moved)));
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

    public static void AddSectionTotal(ExcelSheet sheet, string sectionTitle, IReadOnlyList<ValuationExportLine> lines)
    {
        var counting = lines.Where(line => line.CountsTowardTotals).ToList();
        sheet.AddRow(
            new ExcelStyledCell(null, TotalFill),
            new ExcelStyledCell($"{sectionTitle} total", TotalLabel),
            new ExcelStyledCell(null, TotalFill),
            new ExcelStyledCell(null, TotalFill),
            new ExcelStyledCell(null, TotalFill),
            new ExcelStyledCell(counting.Sum(line => line.LineAmount), TotalMoney),
            new ExcelStyledCell(null, TotalFill),
            new ExcelStyledCell(counting.Sum(line => line.PreviousClaimed), TotalMoney),
            new ExcelStyledCell(counting.Sum(line => line.ThisPeriod), TotalMoney),
            new ExcelStyledCell(counting.Sum(line => line.CumulativeClaimed), TotalMoney));
    }
}
