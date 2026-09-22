using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Documents;

/// <summary>
/// The tables every house-style report is made of: a register (navy header row, hairline
/// borders, one body row per record), a two-pair label/value grid, and the muted line that says
/// a section has nothing in it. A renderer composes these and never re-types a border or a
/// shading.
/// </summary>
public static class DocumentTables
{
    public static Table RegisterTable(Section section, params (string Heading, double Centimetres, bool IsRightAligned)[] columns)
    {
        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        foreach (var column in columns)
        {
            var added = table.AddColumn(Unit.FromCentimeter(column.Centimetres));
            if (column.IsRightAligned) added.Format.Alignment = ParagraphAlignment.Right;
        }
        var header = table.AddRow();
        header.Shading.Color = Navy;
        header.HeadingFormat = true;
        for (var index = 0; index < columns.Length; index++) HeaderCell(header.Cells[index], columns[index].Heading);
        return table;
    }

    public static void BodyRow(Table table, params string[] values)
    {
        var row = table.AddRow();
        for (var index = 0; index < values.Length; index++) BodyCell(row.Cells[index], values[index]);
    }

    public static Table GridTable(Section section)
    {
        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        var labelWidth = Unit.FromCentimeter(3.3);
        var valueWidth = Unit.FromCentimeter(5.6);
        table.AddColumn(labelWidth);
        table.AddColumn(valueWidth);
        table.AddColumn(labelWidth);
        table.AddColumn(valueWidth);
        return table;
    }

    public static void GridRow(Table table, string firstLabel, string firstValue, string secondLabel, string secondValue)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        LabelCell(row.Cells[0], firstLabel);
        ValueCell(row.Cells[1], firstValue);
        LabelCell(row.Cells[2], secondLabel);
        ValueCell(row.Cells[3], secondValue);
    }

    public static void MutedLine(Section section, string text)
    {
        var line = section.AddParagraph(text);
        line.Format.Font.Size = 9;
        line.Format.Font.Italic = true;
        line.Format.Font.Color = Muted;
        SpaceAfter(line, 2);
    }
}
