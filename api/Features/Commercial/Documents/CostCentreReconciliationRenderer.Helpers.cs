using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

public static partial class CostCentreReconciliationRenderer
{
    /// <summary>A lined table with the panel-shaded header row; the last column is the money
    /// column, right-aligned.</summary>
    private static Table AddLinesTable(Section section, params (double WidthCm, string Heading)[] columns)
    {
        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        foreach (var column in columns) table.AddColumn(Unit.FromCentimeter(column.WidthCm));
        table.Columns[columns.Length - 1].Format.Alignment = ParagraphAlignment.Right;
        var header = table.AddRow();
        header.Shading.Color = Panel;
        for (var index = 0; index < columns.Length; index++) HeaderCell(header.Cells[index], columns[index].Heading);
        return table;
    }

    private static Row AddPaddedRow(Table table)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1);
        row.BottomPadding = Unit.FromMillimeter(1);
        return row;
    }

    private static void AddLineRow(Table table, string reference, string description, decimal amount, bool muted = false)
    {
        var row = AddPaddedRow(table);
        TextCell(row.Cells[0], reference, mutedMono: true);
        TextCell(row.Cells[1], description, mutedMono: muted);
        MoneyCell(row.Cells[2], amount);
    }

    private static void AddEmptyRow(Table table, int columns, string message)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        row.Cells[0].MergeRight = columns - 1;
        var p = row.Cells[0].AddParagraph(message);
        p.Format.LeftIndent = Unit.FromMillimeter(1.5);
        p.Format.Font.Size = 8;
        p.Format.Font.Italic = true;
        p.Format.Font.Color = Muted;
    }

    private static void AddTotalRow(Table table, int labelSpan, string label, decimal amount)
    {
        var row = table.AddRow();
        row.Shading.Color = Panel;
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        row.Cells[0].MergeRight = labelSpan - 1;
        var p = row.Cells[0].AddParagraph(label);
        p.Format.LeftIndent = Unit.FromMillimeter(1.5);
        p.Format.Font.Size = 8.5;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Navy;
        MoneyCell(row.Cells[labelSpan], amount, bold: true);
    }

    private static void TextCell(Cell cell, string text, bool mutedMono = false)
    {
        cell.Format.LeftIndent = Unit.FromMillimeter(1.5);
        cell.Format.RightIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(string.IsNullOrWhiteSpace(text) ? "" : text);
        p.Format.Font.Size = mutedMono ? 8 : 8.5;
        p.Format.Font.Color = mutedMono ? Muted : Ink;
    }

    /// <summary>The shared heading plus KeepWithNext, so a table never opens on the page after
    /// its title.</summary>
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

    /// <summary>Muted small caps for a lines table, unlike the shared white-on-navy header cell.</summary>
    private static void HeaderCell(Cell cell, string text)
    {
        cell.Format.LeftIndent = Unit.FromMillimeter(1.5);
        cell.Format.RightIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(text);
        p.Format.Font.Size = 7.5;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Muted;
    }

    private static void MoneyCell(Cell cell, decimal amount, bool bold = false, Color? colour = null)
    {
        cell.Format.RightIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(Money(amount));
        p.Format.Font.Size = 8.5;
        p.Format.Font.Bold = bold;
        p.Format.Font.Color = colour ?? Ink;
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

    /// <summary>Explicit sign and symbol, independent of the process's globalization mode.</summary>
    // U+2212 MINUS SIGN, not the hyphen-minus: MigraDoc breaks a line after a hyphen that is not
    // followed by a digit, so "-£1,000.00" could print as a bare "-" with the figure on the next
    // line (the valuation report did exactly that, 2026-09-02). Same rule as
    // ValuationReportSnapshotRenderer.Money.
    private static string Money(decimal value) => value.ToString("£#,##0.00;\u2212£#,##0.00", Uk);
    private static string Pct(decimal value) => value.ToString("0.#", Uk) + "%";
}
