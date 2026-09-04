using Jewel.JPMS.Contracts.Documents.Excel;

namespace Jewel.JPMS.Contracts.WeeklyCashflow.Export;

/// <summary>
/// The cell styles of the weekly cashflow tabs, in the JewelBB document palette the valuation
/// statements use: navy title band, panel-shaded band totals, hairlines under lines, and the
/// warm highlight on an amount the accountant moved — the export's ‣.
/// </summary>
internal static class WeeklyCashflowExportStyles
{
    public static readonly ExcelCellStyle Band = new(Fill: ExcelFill.Navy);
    public static readonly ExcelCellStyle BandTitle = new(Font: ExcelFont.Title, Fill: ExcelFill.Navy);
    public static readonly ExcelCellStyle BandGold = new(Font: ExcelFont.Gold, Fill: ExcelFill.Navy);
    public static readonly ExcelCellStyle BandGoldRight = new(Font: ExcelFont.Gold, Fill: ExcelFill.Navy, Align: ExcelAlign.Right);
    public static readonly ExcelCellStyle BandTextRight = new(Font: ExcelFont.BandText, Fill: ExcelFill.Navy, Align: ExcelAlign.Right);
    public static readonly ExcelCellStyle Legend = new(Font: ExcelFont.SmallMuted);

    public static readonly ExcelCellStyle SectionHead = new(Font: ExcelFont.NavyBold, Border: ExcelBorder.Accent);
    public static readonly ExcelCellStyle SectionHeadFill = new(Border: ExcelBorder.Accent);
    public static readonly ExcelCellStyle ColumnHead = new(Font: ExcelFont.Muted, Fill: ExcelFill.Panel, Border: ExcelBorder.Hairline);
    public static readonly ExcelCellStyle ColumnHeadRight = ColumnHead with { Align = ExcelAlign.Right };

    // Band totals, net movement and the closing balance: panel-shaded, navy bold figures.
    public static readonly ExcelCellStyle TotalLabel = new(Font: ExcelFont.NavyBold, Fill: ExcelFill.Panel);
    public static readonly ExcelCellStyle TotalFill = new(Fill: ExcelFill.Panel);
    public static readonly ExcelCellStyle TotalMoney = new(Format: ExcelFormat.Currency, Font: ExcelFont.NavyBold, Fill: ExcelFill.Panel);
    public static readonly ExcelCellStyle TotalMoneyNegative = new(Format: ExcelFormat.Currency, Font: ExcelFont.Negative, Fill: ExcelFill.Panel);

    // Lines and the entries beneath them: hairline under every cell so a row reads across.
    public static readonly ExcelCellStyle Line = new(Border: ExcelBorder.Hairline);
    public static readonly ExcelCellStyle LineLabelBold = new(Font: ExcelFont.Bold, Border: ExcelBorder.Hairline);
    public static readonly ExcelCellStyle LineText = new(Font: ExcelFont.Muted, Border: ExcelBorder.Hairline);
    public static readonly ExcelCellStyle LineDate = new(Format: ExcelFormat.Date, Font: ExcelFont.Muted, Border: ExcelBorder.Hairline);

    // The tiles block: label, figure, small-print note.
    public static readonly ExcelCellStyle SummaryLabel = new(Font: ExcelFont.Muted);
    public static readonly ExcelCellStyle SummaryMoney = new(Format: ExcelFormat.Currency, Font: ExcelFont.NavyBold);
    public static readonly ExcelCellStyle SummaryMoneyNegative = new(Format: ExcelFormat.Currency, Font: ExcelFont.Negative);
    public static readonly ExcelCellStyle SummaryNote = new(Font: ExcelFont.SmallMuted);

    // Parked entries: small print, so they are visible without reading as counted.
    public static readonly ExcelCellStyle Excluded = new(Font: ExcelFont.SmallMuted, Border: ExcelBorder.Hairline);

    public static ExcelCellStyle Money(bool isMoved, bool isNegative) => new(
        Format: ExcelFormat.Currency,
        Font: isNegative ? ExcelFont.Negative : ExcelFont.Default,
        Border: ExcelBorder.Hairline,
        Fill: isMoved ? ExcelFill.Highlight : ExcelFill.None);

    public static ExcelCellStyle TotalMoneyFor(decimal amount) => amount < 0m ? TotalMoneyNegative : TotalMoney;

    public static ExcelCellStyle SummaryMoneyFor(decimal amount) => amount < 0m ? SummaryMoneyNegative : SummaryMoney;
}
