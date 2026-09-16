using DocumentFormat.OpenXml.Wordprocessing;

using static Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents.ContractorsReportWordParts;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

/// <summary>The report's two table shapes in Word: the label/value grid and the register
/// table with its navy header row. Widths are twips; the page's usable width is 17.8 cm.</summary>
internal static class ContractorsReportWordTables
{
    public sealed record RegisterColumn(string Heading, double Centimetres, bool IsRightAligned = false);

    private const int CellPaddingTwips = 60;

    public static Table Register(IReadOnlyList<RegisterColumn> columns)
    {
        var table = Bordered(columns.Select(column => column.Centimetres).ToList());
        var header = new TableRow(new TableRowProperties(new TableHeader()));
        foreach (var column in columns)
            header.Append(Cell(Text(column.Heading, SmallSize, true, false, White, 0), column, Navy));
        table.Append(header);
        return table;
    }

    public static void BodyRow(Table table, IReadOnlyList<RegisterColumn> columns, params string[] values)
    {
        var row = new TableRow();
        for (var index = 0; index < columns.Count; index++)
            row.Append(Cell(Text(ContractorsReportText.OrDash(values[index]), SmallSize, false, false, Ink, 0), columns[index], null));
        table.Append(row);
    }

    public static void TotalRow(Table table, IReadOnlyList<RegisterColumn> columns, int labelAt, string label, string total)
    {
        var row = new TableRow();
        for (var index = 0; index < columns.Count; index++)
        {
            var text = index == labelAt ? label : index == labelAt + 1 ? total : "";
            row.Append(Cell(Text(text, SmallSize, true, false, Muted, 0), columns[index], PanelFill));
        }
        table.Append(row);
    }

    public static Table Grid(params (string Label, string Value)[] pairs)
    {
        var widths = new[] { 3.3, 5.6, 3.3, 5.6 };
        var table = Bordered(widths);
        for (var index = 0; index < pairs.Length; index += 2)
        {
            var row = new TableRow();
            AppendPair(row, pairs[index], widths[0], widths[1]);
            if (index + 1 < pairs.Length) AppendPair(row, pairs[index + 1], widths[2], widths[3]);
            table.Append(row);
        }
        return table;
    }

    private static void AppendPair(TableRow row, (string Label, string Value) pair, double labelWidth, double valueWidth)
    {
        if (string.IsNullOrEmpty(pair.Label))
        {
            row.Append(Cell(new Paragraph(), new RegisterColumn("", labelWidth), null));
            row.Append(Cell(new Paragraph(), new RegisterColumn("", valueWidth), null));
            return;
        }
        row.Append(Cell(Text(pair.Label, SmallSize, true, false, Muted, 0), new RegisterColumn("", labelWidth), PanelFill));
        row.Append(Cell(Text(ContractorsReportText.OrDash(pair.Value), BodySize, false, false, Ink, 0), new RegisterColumn("", valueWidth), null));
    }

    private static Table Bordered(IReadOnlyList<double> centimetres)
    {
        var table = new Table();
        var properties = new TableProperties
        {
            TableWidth = new TableWidth { Width = Twips(centimetres.Sum()).ToString(), Type = TableWidthUnitValues.Dxa },
            TableBorders = new TableBorders(
                new TopBorder { Val = BorderValues.Single, Color = HairFill, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Color = HairFill, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Color = HairFill, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Color = HairFill, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Color = HairFill, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Color = HairFill, Size = 4 }),
            TableCellMarginDefault = new TableCellMarginDefault(
                new TopMargin { Width = CellPaddingTwips.ToString(), Type = TableWidthUnitValues.Dxa },
                new TableCellLeftMargin { Width = (short)CellPaddingTwips, Type = TableWidthValues.Dxa },
                new BottomMargin { Width = CellPaddingTwips.ToString(), Type = TableWidthUnitValues.Dxa },
                new TableCellRightMargin { Width = (short)CellPaddingTwips, Type = TableWidthValues.Dxa })
        };
        table.Append(properties);
        table.Append(new TableGrid(centimetres.Select(width => new GridColumn { Width = Twips(width).ToString() })));
        return table;
    }

    private static TableCell Cell(Paragraph paragraph, RegisterColumn column, string? fill)
    {
        if (column.IsRightAligned) Aligned(paragraph, JustificationValues.Right);
        var properties = new TableCellProperties
        {
            TableCellWidth = new TableCellWidth { Width = Twips(column.Centimetres).ToString(), Type = TableWidthUnitValues.Dxa }
        };
        if (fill is not null) properties.Shading = new Shading { Val = ShadingPatternValues.Clear, Fill = fill };
        return new TableCell(properties, paragraph);
    }

    public static int Twips(double centimetres) => (int)Math.Round(centimetres * TwipsPerCentimetre);
}
