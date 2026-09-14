using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

public static partial class ProjectSupplierAccountRenderer
{
    private enum TextEmphasis { Normal, Bold, Reference, Muted, Indented, IndentedNote }

    private static readonly Unit IndentWidth = Unit.FromMillimeter(3);

    private static void TextCell(Cell cell, string text, TextEmphasis emphasis = TextEmphasis.Normal, bool alignRight = false)
    {
        cell.Format.LeftIndent = CellIndent;
        cell.Format.RightIndent = CellIndent;
        var paragraph = cell.AddParagraph(text);
        if (alignRight) paragraph.Format.Alignment = ParagraphAlignment.Right;
        var isNote = emphasis is TextEmphasis.Muted or TextEmphasis.Indented or TextEmphasis.IndentedNote;
        paragraph.Format.Font.Size = isNote ? 8 : 8.5;
        paragraph.Format.Font.Bold = emphasis is TextEmphasis.Bold or TextEmphasis.Reference;
        paragraph.Format.Font.Italic = emphasis is TextEmphasis.IndentedNote;
        paragraph.Format.Font.Color = emphasis is TextEmphasis.Reference ? Navy : isNote ? Muted : Ink;
        if (emphasis is TextEmphasis.Indented or TextEmphasis.IndentedNote) paragraph.Format.LeftIndent = IndentWidth;
    }

    private static void MoneyCell(Cell cell, decimal amount, bool bold = false, Color? colour = null)
    {
        cell.Format.RightIndent = CellIndent;
        var paragraph = cell.AddParagraph(Money(amount));
        paragraph.Format.Font.Size = 8.5;
        paragraph.Format.Font.Bold = bold;
        paragraph.Format.Font.Color = colour ?? Ink;
    }

    private static void AddGridRow(Table table, string leftLabel, string leftValue, string rightLabel, string rightValue)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        LabelCell(row.Cells[0], leftLabel);
        ValueCell(row.Cells[1], leftValue);
        LabelCell(row.Cells[2], rightLabel);
        ValueCell(row.Cells[3], rightValue);
    }

    /// <summary>The shared heading plus KeepWithNext, so a table never opens on the page after its title.</summary>
    private static void SectionHeading(Section section, string text)
    {
        var paragraph = section.AddParagraph(text);
        paragraph.Format.Font.Size = 10.5;
        paragraph.Format.Font.Bold = true;
        paragraph.Format.Font.Color = Navy;
        paragraph.Format.Borders.Bottom.Width = 0.75;
        paragraph.Format.Borders.Bottom.Color = Orange;
        paragraph.Format.Borders.Distance = Unit.FromMillimeter(1.5);
        paragraph.Format.KeepWithNext = true;
        SpaceBefore(paragraph, 4);
        SpaceAfter(paragraph, 2.5);
    }

    /// <summary>Muted small caps for a lines table, unlike the shared white-on-navy header cell.</summary>
    private static void HeaderCell(Cell cell, string text)
    {
        cell.Format.LeftIndent = CellIndent;
        cell.Format.RightIndent = CellIndent;
        var paragraph = cell.AddParagraph(text);
        paragraph.Format.Font.Size = 7.5;
        paragraph.Format.Font.Bold = true;
        paragraph.Format.Font.Color = Muted;
    }

    private static Color? NegativeWhenBelowZero(decimal amount) => amount < 0m ? Negative : null;

    // U+2212 MINUS SIGN, not the hyphen-minus: MigraDoc breaks a line after a hyphen that is not
    // followed by a digit, so "-£1,000.00" could print as a bare "-" with the figure on the next
    // line. Same rule as ValuationReportSnapshotRenderer.Money.
    private static string Money(decimal value) => value.ToString("£#,##0.00;\u2212£#,##0.00", Uk);
}
