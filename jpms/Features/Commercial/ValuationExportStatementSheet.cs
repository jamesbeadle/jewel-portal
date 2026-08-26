using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Services.Excel;
using static Jewel.JPMS.Features.Commercial.ValuationExportStyles;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>How one branded statement tab reads: its name, the legend line under the title band, and
/// the sub-heading a line sits under ("" = no sub-heading, or continue the one above).</summary>
public sealed record ValuationExportStatementLayout(string SheetName, string Legend, Func<ValuationExportLine, string> BandTitle);

/// <summary>
/// Writes one branded statement tab: navy title band, sectioned bill with Previous / This
/// period / Claimed columns, gold-shaded moved lines, summary footer. The Summary and Detail
/// tabs are the same sheet fed different rows.
/// </summary>
internal static class ValuationExportStatementSheet
{
    public static void Add(
        ExcelWorkbook workbook,
        ValuationExportStatementLayout layout,
        ValuationExportMeta meta,
        IReadOnlyList<ValuationExportLine> lines,
        IReadOnlyList<ValuationExportSummaryRow> summary)
    {
        var sheet = workbook.AddSheet(layout.SheetName,
            new ExcelColumn("Code", Width: 13),
            new ExcelColumn("Description", Width: 52),
            new ExcelColumn("Unit", Width: 7),
            new ExcelColumn("Qty", Width: 9),
            new ExcelColumn("Rate", Width: 11),
            new ExcelColumn("Amount", Width: 14),
            new ExcelColumn("% Complete", Width: 11),
            new ExcelColumn("Previous", Width: 14),
            new ExcelColumn("This period", Width: 14),
            new ExcelColumn("Claimed", Width: 14));
        sheet.ShowHeaderRow = false;
        sheet.AutoFilter = false;
        sheet.FreezeHeaderRow = false;
        sheet.ShowGridLines = false;
        sheet.PrintLandscapeFitToWidth = true;

        ValuationExportTitleBand.Add(sheet, meta, layout.Legend);
        foreach (var section in lines.GroupBy(line => line.Section))
        {
            AddSection(sheet, section.Key, section.ToList(), layout.BandTitle);
        }
        ValuationExportSummaryFooter.Add(sheet, summary);
    }

    // A heading in the first cell with the fill (or accent border) carried across every other
    // cell, unmerged, so the band's border and shading render on each cell.
    private static void AddHeadingRow(ExcelSheet sheet, ExcelStyledCell first, ExcelCellStyle fill)
    {
        var cells = FilledCells(fill);
        cells[0] = first;
        sheet.AddRow(cells);
    }

    private static object?[] FilledCells(ExcelCellStyle fill) =>
        Enumerable.Repeat<object?>(new ExcelStyledCell(null, fill), ValuationExportTitleBand.ColumnCount).ToArray();

    private static void AddSection(
        ExcelSheet sheet, string title, IReadOnlyList<ValuationExportLine> lines, Func<ValuationExportLine, string> bandTitleFor)
    {
        AddHeadingRow(sheet, new ExcelStyledCell(title, SectionHead), SectionHeadFill);
        ValuationExportBillRows.AddColumnHeadings(sheet);
        var currentBand = "";
        foreach (var line in lines)
        {
            var band = bandTitleFor(line);
            if (ValuationReportAreas.StartsNewArea(band, currentBand))
            {
                currentBand = band;
                AddHeadingRow(sheet, new ExcelStyledCell(band, BandHead), BandFill);
            }
            ValuationExportBillRows.AddLine(sheet, line);
        }
        ValuationExportBillRows.AddSectionTotal(sheet, title, lines);
        sheet.AddRow();
    }
}
