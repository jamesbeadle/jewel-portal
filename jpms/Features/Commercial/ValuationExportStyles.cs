using Jewel.JPMS.Services.Excel;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>
/// The cell styles of the branded statement tabs (the writer deduplicates them): navy title
/// band, panel-shaded area and total rows, gold-shaded lines that moved this period.
/// </summary>
internal static class ValuationExportStyles
{
    public static readonly ExcelCellStyle Band = new(Fill: ExcelFill.Navy);
    public static readonly ExcelCellStyle BandTitle = new(Font: ExcelFont.Title, Fill: ExcelFill.Navy);
    public static readonly ExcelCellStyle BandGold = new(Font: ExcelFont.Gold, Fill: ExcelFill.Navy);
    public static readonly ExcelCellStyle BandGoldRight = new(Font: ExcelFont.Gold, Fill: ExcelFill.Navy, Align: ExcelAlign.Right);
    public static readonly ExcelCellStyle BandTextRight = new(Font: ExcelFont.BandText, Fill: ExcelFill.Navy, Align: ExcelAlign.Right);
    public static readonly ExcelCellStyle DraftWarning = new(Font: ExcelFont.Negative);
    public static readonly ExcelCellStyle Legend = new(Font: ExcelFont.SmallMuted);
    public static readonly ExcelCellStyle SectionHead = new(Font: ExcelFont.NavyBold, Border: ExcelBorder.Accent);
    public static readonly ExcelCellStyle SectionHeadFill = new(Border: ExcelBorder.Accent);
    public static readonly ExcelCellStyle ColHead = new(Font: ExcelFont.Muted, Fill: ExcelFill.Panel, Border: ExcelBorder.Hairline);
    public static readonly ExcelCellStyle ColHeadRight = ColHead with { Align = ExcelAlign.Right };

    // Area / variation-order sub-headings within a section — a panel band with the title on
    // the left, so the bill reads in the estimate's own areas ("Electrics", "Plumbing & Heating").
    public static readonly ExcelCellStyle BandHead = new(Font: ExcelFont.NavyBold, Fill: ExcelFill.Panel);
    public static readonly ExcelCellStyle BandFill = new(Fill: ExcelFill.Panel);

    public static readonly ExcelCellStyle TotalLabel = new(Font: ExcelFont.NavyBold, Fill: ExcelFill.Panel);
    public static readonly ExcelCellStyle TotalFill = new(Fill: ExcelFill.Panel);
    public static readonly ExcelCellStyle TotalMoney = new(Format: ExcelFormat.Currency, Font: ExcelFont.NavyBold, Fill: ExcelFill.Panel);
    public static readonly ExcelCellStyle SummaryLabel = new(Font: ExcelFont.Muted);
    public static readonly ExcelCellStyle SummaryMoney = new(Format: ExcelFormat.Currency);
    public static readonly ExcelCellStyle SummaryMoneyNegative = SummaryMoney with { Font = ExcelFont.Negative };
    public static readonly ExcelCellStyle SummaryLabelStrong = new(Font: ExcelFont.NavyBold, Fill: ExcelFill.Panel);
    public static readonly ExcelCellStyle SummaryFillStrong = new(Fill: ExcelFill.Panel);
    public static readonly ExcelCellStyle SummaryMoneyStrong = new(Format: ExcelFormat.Currency, Font: ExcelFont.NavyBold, Fill: ExcelFill.Panel);
    public static readonly ExcelCellStyle Plain = new();

    public static ExcelCellStyle Text(bool moved) => new(Border: ExcelBorder.Hairline, Fill: FillFor(moved));
    public static ExcelCellStyle Desc(bool moved) => new(Border: ExcelBorder.Hairline, Fill: FillFor(moved), WrapText: true);
    public static ExcelCellStyle Code(bool moved) => new(Font: ExcelFont.Muted, Border: ExcelBorder.Hairline, Fill: FillFor(moved));
    public static ExcelCellStyle Num(bool moved) => new(Format: ExcelFormat.Number, Border: ExcelBorder.Hairline, Fill: FillFor(moved));
    public static ExcelCellStyle Pct(bool moved) => new(Format: ExcelFormat.Percent, Border: ExcelBorder.Hairline, Fill: FillFor(moved));

    public static ExcelCellStyle Money(bool moved, bool negative = false, bool strong = false) => new(
        Format: ExcelFormat.Currency,
        Font: negative ? ExcelFont.Negative : strong ? ExcelFont.NavyBold : ExcelFont.Default,
        Border: ExcelBorder.Hairline,
        Fill: FillFor(moved));

    private static ExcelFill FillFor(bool moved) => moved ? ExcelFill.Highlight : ExcelFill.None;
}
