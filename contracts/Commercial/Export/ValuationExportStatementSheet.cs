using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Documents.Excel;
using Jewel.JPMS.Models;
using static Jewel.JPMS.Contracts.Commercial.Export.ValuationExportStyles;

namespace Jewel.JPMS.Contracts.Commercial.Export;

/// <summary>How one branded statement tab reads: its name, the legend line under the title band, and
/// the sub-heading a line sits under ("" = no sub-heading, or continue the one above).</summary>
public sealed record ValuationExportStatementLayout(string SheetName, string Legend, Func<ValuationExportLine, string> BandTitle);

/// <summary>
/// Writes one branded statement tab: navy title band, sectioned bill with Previous / This
/// period / Claimed columns, gold-shaded moved lines, summary footer. The per-variation tabs
/// borrow its column grid and heading-row machinery so every tab reads as one document.
/// </summary>
internal static class ValuationExportStatementSheet
{
    /// <summary>The statement's bill grid — shared with the per-variation tabs. The client's
    /// schedule-of-works reference column exists only when the export's lines carry one.</summary>
    public static ExcelColumn[] BillColumns(bool hasClientReference)
    {
        var columns = new List<ExcelColumn>
        {
            new("Code", Width: 13),
            new("Description", Width: hasClientReference ? 46 : 52),
            new("Unit", Width: 7),
            new("Qty", Width: 9),
            new("Rate", Width: 11),
            new("Amount", Width: 14),
            new("% Complete", Width: 11),
            new("Previous", Width: 14),
            new("This period", Width: 14),
            new("Claimed", Width: 14),
        };
        if (hasClientReference) columns.Insert(1, new ExcelColumn("Client ref", Width: 10));
        return columns.ToArray();
    }

    /// <summary>Turn off the data-table dressing — these are presentation sheets.</summary>
    public static void SetPresentationFlags(ExcelSheet sheet)
    {
        sheet.ShowHeaderRow = false;
        sheet.AutoFilter = false;
        sheet.FreezeHeaderRow = false;
        sheet.ShowGridLines = false;
        sheet.PrintLandscapeFitToWidth = true;
    }

    public static void Add(
        ExcelWorkbook workbook,
        ValuationExportStatementLayout layout,
        ValuationExportMeta meta,
        IReadOnlyList<ValuationExportLine> lines,
        IReadOnlyList<ValuationExportSummaryRow> summary,
        bool hasClientReference)
    {
        var sheet = workbook.AddSheet(layout.SheetName, BillColumns(hasClientReference));
        SetPresentationFlags(sheet);

        ValuationExportTitleBand.Add(sheet, meta, layout.Legend);
        foreach (var section in lines.GroupBy(line => line.Section))
        {
            AddSection(sheet, section.Key, section.ToList(), layout.BandTitle, hasClientReference);
        }
        ValuationExportSummaryFooter.Add(sheet, summary);
    }

    // A heading in the first cell with the fill (or accent border) carried across every other
    // cell, unmerged, so the band's border and shading render on each cell.
    public static void AddHeadingRow(ExcelSheet sheet, ExcelStyledCell first, ExcelCellStyle fill)
    {
        var cells = FilledCells(sheet, fill);
        cells[0] = first;
        sheet.AddRow(cells);
    }

    public static object?[] FilledCells(ExcelSheet sheet, ExcelCellStyle fill) =>
        Enumerable.Repeat<object?>(new ExcelStyledCell(null, fill), sheet.Columns.Count).ToArray();

    private static void AddSection(
        ExcelSheet sheet, string title, IReadOnlyList<ValuationExportLine> lines,
        Func<ValuationExportLine, string> bandTitleFor, bool hasClientReference)
    {
        AddHeadingRow(sheet, new ExcelStyledCell(title, SectionHead), SectionHeadFill);
        ValuationExportBillRows.AddColumnHeadings(sheet, hasClientReference);
        var currentBand = "";
        foreach (var line in lines)
        {
            var band = bandTitleFor(line);
            if (ValuationReportAreas.StartsNewArea(band, currentBand))
            {
                currentBand = band;
                AddHeadingRow(sheet, new ExcelStyledCell(band, BandHead), BandFill);
            }
            ValuationExportBillRows.AddLine(sheet, line, hasClientReference);
        }
        ValuationExportBillRows.AddSectionTotal(sheet, title, lines);
        sheet.AddRow();
    }
}
