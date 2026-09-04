using System.Globalization;
using System.Text;

namespace Jewel.JPMS.Contracts.Documents.Excel;

public static partial class ExcelWorkbookWriter
{
    private static string SheetXml(ExcelSheet sheet, ExcelStyleRegistry styles)
    {
        var columnCount = sheet.Columns.Count;
        var lastColumn = ColumnLetter(columnCount);
        var headerRows = sheet.ShowHeaderRow ? 1 : 0;
        var lastRow = sheet.Rows.Count + headerRows;

        var builder = new StringBuilder();
        builder.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        builder.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");
        if (sheet.PrintLandscapeFitToWidth || sheet.TabColour is not null)
        {
            // Schema order inside sheetPr: tabColor before pageSetUpPr.
            builder.Append("<sheetPr>");
            if (sheet.TabColour is not null)
            {
                builder.Append($"""<tabColor rgb="{sheet.TabColour}"/>""");
            }
            if (sheet.PrintLandscapeFitToWidth)
            {
                builder.Append("""<pageSetUpPr fitToPage="1"/>""");
            }
            builder.Append("</sheetPr>");
        }
        builder.Append($"""<dimension ref="A1:{lastColumn}{Math.Max(lastRow, 1)}"/>""");

        builder.Append("<sheetViews><sheetView workbookViewId=\"0\"");
        if (!sheet.ShowGridLines) builder.Append(" showGridLines=\"0\"");
        var pane = FrozenPaneXml(sheet);
        builder.Append(pane is null ? "/></sheetViews>" : $">{pane}</sheetView></sheetViews>");
        builder.Append("""<sheetFormatPr defaultRowHeight="15"/>""");

        builder.Append("<cols>");
        for (var c = 0; c < columnCount; c++)
        {
            var width = sheet.Columns[c].Width ?? EstimateWidth(sheet, c);
            builder.Append($"""<col min="{c + 1}" max="{c + 1}" width="{width.ToString("0.##", CultureInfo.InvariantCulture)}" customWidth="1"/>""");
        }
        builder.Append("</cols>");

        builder.Append("<sheetData>");

        if (sheet.ShowHeaderRow)
        {
            builder.Append("""<row r="1">""");
            for (var c = 0; c < columnCount; c++)
            {
                AppendInlineString(builder, $"{ColumnLetter(c + 1)}1", styles.Header, sheet.Columns[c].Header);
            }
            builder.Append("</row>");
        }

        for (var r = 0; r < sheet.Rows.Count; r++)
        {
            var rowRef = r + 1 + headerRows;
            var cells = sheet.Rows[r];
            var height = EstimateRowHeight(sheet, cells);
            builder.Append(height is null
                ? $"""<row r="{rowRef}">"""
                : $"""<row r="{rowRef}" ht="{height.Value.ToString("0.##", CultureInfo.InvariantCulture)}" customHeight="1">""");
            for (var c = 0; c < columnCount && c < cells.Length; c++)
            {
                AppendCell(builder, $"{ColumnLetter(c + 1)}{rowRef}", sheet.Columns[c].Format, cells[c], styles);
            }
            builder.Append("</row>");
        }

        builder.Append("</sheetData>");
        if (sheet.ShowHeaderRow && sheet.AutoFilter)
        {
            builder.Append($"""<autoFilter ref="A1:{lastColumn}{lastRow}"/>""");
        }
        if (sheet.MergedRanges.Count > 0)
        {
            builder.Append($"""<mergeCells count="{sheet.MergedRanges.Count}">""");
            foreach (var range in sheet.MergedRanges)
            {
                builder.Append($"""<mergeCell ref="{Escape(range)}"/>""");
            }
            builder.Append("</mergeCells>");
        }
        if (sheet.PrintLandscapeFitToWidth)
        {
            builder.Append("""<pageMargins left="0.4" right="0.4" top="0.5" bottom="0.5" header="0.3" footer="0.3"/>""");
            builder.Append("""<pageSetup orientation="landscape" fitToWidth="1" fitToHeight="0"/>""");
        }
        builder.Append("</worksheet>");
        return builder.ToString();
    }

    /// <summary>
    /// Rows with wrapping presentation cells get an explicit height (Excel does not auto-fit
    /// wrapped text written by other tools): estimated line count times the default line height.
    /// </summary>
    private static double? EstimateRowHeight(ExcelSheet sheet, object?[] cells)
    {
        double? height = null;
        for (var c = 0; c < sheet.Columns.Count && c < cells.Length; c++)
        {
            if (cells[c] is not ExcelStyledCell { Style.WrapText: true } styled) continue;
            var text = styled.Value?.ToString() ?? "";
            if (text.Length == 0) continue;
            var charsPerLine = Math.Max(8.0, (sheet.Columns[c].Width ?? 10) - 2);
            var lines = Math.Clamp((int)Math.Ceiling(text.Length / charsPerLine), 1, 12);
            var estimate = lines * 14.6 + 2;
            if (height is null || estimate > height) height = estimate;
        }
        return height;
    }
}
