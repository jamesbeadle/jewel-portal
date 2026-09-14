using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

public static partial class ProjectSupplierAccountRenderer
{
    /// <summary>A lined table with the panel-shaded, muted header row.</summary>
    private static Table AddLinesTable(Section section, params (double WidthCm, string Heading)[] columns)
    {
        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        foreach (var column in columns) table.AddColumn(Unit.FromCentimeter(column.WidthCm));
        var header = table.AddRow();
        header.Shading.Color = Panel;
        header.HeadingFormat = true;
        for (var index = 0; index < columns.Length; index++) HeaderCell(header.Cells[index], columns[index].Heading);
        return table;
    }

    private static void SetMoneyColumns(Table table, params int[] columnIndexes)
    {
        foreach (var index in columnIndexes) table.Columns[index].Format.Alignment = ParagraphAlignment.Right;
    }

    private static Row AddPaddedRow(Table table)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1);
        row.BottomPadding = Unit.FromMillimeter(1);
        return row;
    }

    private static void AddEmptyRow(Table table, int columnCount, string message)
    {
        var row = AddPaddedRow(table);
        row.Cells[0].MergeRight = columnCount - 1;
        TextCell(row.Cells[0], message, emphasis: TextEmphasis.IndentedNote);
    }

    private static Row AddTotalRow(Table table, int firstMoneyColumn, string label, decimal amount)
    {
        var row = table.AddRow();
        row.Shading.Color = Panel;
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        row.Cells[0].MergeRight = firstMoneyColumn - 1;
        var paragraph = row.Cells[0].AddParagraph(label);
        paragraph.Format.LeftIndent = CellIndent;
        paragraph.Format.Font.Size = 8.5;
        paragraph.Format.Font.Bold = true;
        paragraph.Format.Font.Color = Navy;
        MoneyCell(row.Cells[firstMoneyColumn], amount, bold: true);
        return row;
    }
}
