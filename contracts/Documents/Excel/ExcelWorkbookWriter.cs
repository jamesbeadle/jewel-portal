using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Jewel.JPMS.Contracts.Documents.Excel;

/// <summary>
/// Writes an <see cref="ExcelWorkbook"/> as a real .xlsx file (SpreadsheetML in a zip)
/// with no external dependencies, keeping the WASM payload small. Data sheets get a
/// bold frozen header row, an autofilter, sensible column widths, and per-column
/// number formats; presentation sheets (built from <see cref="ExcelStyledCell"/>s)
/// additionally get fills, fonts, borders, merges and print setup from a style
/// registry that grows only with the styles actually used.
/// </summary>
public static partial class ExcelWorkbookWriter
{
    public static byte[] Write(ExcelWorkbook workbook)
    {
        if (workbook.Sheets.Count == 0)
        {
            throw new InvalidOperationException("Cannot export a workbook with no sheets.");
        }

        // Sheets render FIRST so the style registry has seen every styled cell by the
        // time styles.xml is written; entry order inside the zip is irrelevant to Excel.
        var styles = new ExcelStyleRegistry();
        var sheetXml = workbook.Sheets.Select(sheet => SheetXml(sheet, styles)).ToList();

        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(zip, "[Content_Types].xml", ContentTypesXml(workbook.Sheets.Count));
            AddEntry(zip, "_rels/.rels", RootRelsXml());
            AddEntry(zip, "xl/workbook.xml", WorkbookXml(workbook));
            AddEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRelsXml(workbook.Sheets.Count));
            AddEntry(zip, "xl/styles.xml", styles.ToXml());

            for (var i = 0; i < sheetXml.Count; i++)
            {
                AddEntry(zip, $"xl/worksheets/sheet{i + 1}.xml", sheetXml[i]);
            }
        }

        return stream.ToArray();
    }
}
