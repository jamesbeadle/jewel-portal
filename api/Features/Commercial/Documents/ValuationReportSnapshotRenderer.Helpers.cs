using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

public static partial class ValuationReportSnapshotRenderer
{
    private static void SectionHeading(Section section, string text)
    {
        var p = section.AddParagraph(text);
        p.Format.Font.Size = 10.5;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Navy;
        p.Format.Borders.Bottom.Width = 0.75;
        p.Format.Borders.Bottom.Color = Orange;
        p.Format.Borders.Distance = Unit.FromMillimeter(1.5);
        p.Format.KeepWithNext = true;
        SpaceBefore(p, 4);
        SpaceAfter(p, 2.5);
    }

    private static void HeaderCell(Cell cell, string text)
    {
        cell.Format.LeftIndent = Unit.FromMillimeter(1.5);
        cell.Format.RightIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(text);
        p.Format.Font.Size = 7.5;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Muted;
    }

    // Bill money never wraps: the column is sized (ValuationReportBillColumns) so a six-figure
    // negative fits at the line size and a seven-figure total at the bold size — bold glyphs are
    // wider, so totals drop half a point to stay inside the same column.
    private const double LineMoneySize = 7.5;
    private const double TotalMoneySize = 7;

    private static void MoneyCell(Cell cell, decimal amount, bool bold = false, Color? colour = null)
    {
        cell.Format.RightIndent = Unit.FromMillimeter(1);
        var p = cell.AddParagraph(Money(amount));
        p.Format.Font.Size = bold ? TotalMoneySize : LineMoneySize;
        p.Format.Font.Bold = bold;
        p.Format.Font.Color = colour ?? Ink;
    }

    // Quantity / rate: a consolidated variation row has neither, and prints a dash instead.
    private static void NumberCell(Cell cell, decimal? value)
    {
        var p = cell.AddParagraph(value is { } number ? Num(number) : "—");
        p.Format.Font.Size = 8;
        p.Format.Font.Color = Muted;
    }

    private static void DashCell(Cell cell)
    {
        cell.Format.RightIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph("—");
        p.Format.Font.Size = 8;
        p.Format.Font.Color = Muted;
    }

    private static void AddGridRow(Table table, string l1, string v1, string l2, string v2)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        LabelCell(row.Cells[0], l1);
        ValueCell(row.Cells[1], v1);
        LabelCell(row.Cells[2], l2);
        ValueCell(row.Cells[3], v2);
    }

    // A negative carries U+2212 MINUS SIGN, not the hyphen-minus: MigraDoc treats a hyphen as a
    // line-break opportunity unless a digit follows it directly, and "-£" puts the pound sign
    // in between — so a negative that did not fit its column printed as "-" on one line and
    // "£10,573.80" on the next (accountant 2026-09-02). The minus sign is a plain glyph to the
    // layout engine, so the figure and its sign stay one word. Every face the font resolver
    // can pick (DejaVu, Liberation, Lato, Arial…) carries it.
    internal const char MinusSign = '−';
    internal static string Money(decimal value) => value.ToString($"£#,##0.00;{MinusSign}£#,##0.00", Uk);
    private static string Num(decimal value) => value.ToString("0.##", Uk);
    private static string Pct(decimal value) => value.ToString("0.##", Uk) + "%";
}
