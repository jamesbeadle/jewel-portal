using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Jewel.JPMS.Services.Excel;

/// <summary>
/// Writes an <see cref="ExcelWorkbook"/> as a real .xlsx file (SpreadsheetML in a zip)
/// with no external dependencies, keeping the WASM payload small. Every sheet gets a
/// bold frozen header row, an autofilter, sensible column widths, and per-column
/// number formats (currency, dates, percentages).
/// </summary>
public static class ExcelWorkbookWriter
{
    public static byte[] Write(ExcelWorkbook workbook)
    {
        if (workbook.Sheets.Count == 0)
        {
            throw new InvalidOperationException("Cannot export a workbook with no sheets.");
        }

        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(zip, "[Content_Types].xml", ContentTypesXml(workbook.Sheets.Count));
            AddEntry(zip, "_rels/.rels", RootRelsXml());
            AddEntry(zip, "xl/workbook.xml", WorkbookXml(workbook));
            AddEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRelsXml(workbook.Sheets.Count));
            AddEntry(zip, "xl/styles.xml", StylesXml());

            for (var i = 0; i < workbook.Sheets.Count; i++)
            {
                AddEntry(zip, $"xl/worksheets/sheet{i + 1}.xml", SheetXml(workbook.Sheets[i]));
            }
        }

        return stream.ToArray();
    }

    // ----- package parts -------------------------------------------------

    private static void AddEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static string ContentTypesXml(int sheetCount)
    {
        var builder = new StringBuilder();
        builder.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        builder.Append("""<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">""");
        builder.Append("""<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>""");
        builder.Append("""<Default Extension="xml" ContentType="application/xml"/>""");
        builder.Append("""<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>""");
        builder.Append("""<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>""");
        for (var i = 1; i <= sheetCount; i++)
        {
            builder.Append($"""<Override PartName="/xl/worksheets/sheet{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>""");
        }
        builder.Append("</Types>");
        return builder.ToString();
    }

    private static string RootRelsXml() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
        """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""" +
        """<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>""" +
        "</Relationships>";

    private static string WorkbookXml(ExcelWorkbook workbook)
    {
        var builder = new StringBuilder();
        builder.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        builder.Append("""<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">""");
        builder.Append("<sheets>");
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < workbook.Sheets.Count; i++)
        {
            var name = SanitizeSheetName(workbook.Sheets[i].Name, i, usedNames);
            builder.Append($"""<sheet name="{Escape(name)}" sheetId="{i + 1}" r:id="rId{i + 1}"/>""");
        }
        builder.Append("</sheets></workbook>");
        return builder.ToString();
    }

    private static string WorkbookRelsXml(int sheetCount)
    {
        var builder = new StringBuilder();
        builder.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        builder.Append("""<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""");
        for (var i = 1; i <= sheetCount; i++)
        {
            builder.Append($"""<Relationship Id="rId{i}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet{i}.xml"/>""");
        }
        builder.Append($"""<Relationship Id="rId{sheetCount + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>""");
        builder.Append("</Relationships>");
        return builder.ToString();
    }

    // Style indexes referenced from SheetXml. Order matters — keep in sync with StylesXml.
    private const int StyleDefault = 0;
    private const int StyleHeader = 1;
    private static int StyleFor(ExcelFormat format) => format switch
    {
        ExcelFormat.Integer  => 2,
        ExcelFormat.Number   => 3,
        ExcelFormat.Currency => 4,
        ExcelFormat.Date     => 5,
        ExcelFormat.DateTime => 6,
        ExcelFormat.Percent  => 7,
        _                    => StyleDefault,
    };

    private static string StylesXml() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
        """<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""" +
        """<numFmts count="6">""" +
        """<numFmt numFmtId="164" formatCode="#,##0"/>""" +
        """<numFmt numFmtId="165" formatCode="#,##0.00"/>""" +
        """<numFmt numFmtId="166" formatCode="&quot;£&quot;#,##0.00"/>""" +
        """<numFmt numFmtId="167" formatCode="dd/mm/yyyy"/>""" +
        """<numFmt numFmtId="168" formatCode="dd/mm/yyyy\ hh:mm"/>""" +
        """<numFmt numFmtId="169" formatCode="0.0%"/>""" +
        "</numFmts>" +
        """<fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts>""" +
        """<fills count="3"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill>""" +
        """<fill><patternFill patternType="solid"><fgColor rgb="FFF2F1EE"/></patternFill></fill></fills>""" +
        """<borders count="2"><border><left/><right/><top/><bottom/><diagonal/></border>""" +
        """<border><left/><right/><top/><bottom style="thin"><color rgb="FFB9B6B0"/></bottom><diagonal/></border></borders>""" +
        """<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>""" +
        """<cellXfs count="8">""" +
        """<xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>""" +                                          // 0 default / text
        """<xf numFmtId="0" fontId="1" fillId="2" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1"/>""" + // 1 header
        """<xf numFmtId="164" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>""" +                   // 2 integer
        """<xf numFmtId="165" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>""" +                   // 3 number
        """<xf numFmtId="166" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>""" +                   // 4 currency
        """<xf numFmtId="167" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>""" +                   // 5 date
        """<xf numFmtId="168" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>""" +                   // 6 datetime
        """<xf numFmtId="169" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>""" +                   // 7 percent
        "</cellXfs>" +
        """<cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>""" +
        "</styleSheet>";

    // ----- worksheet ------------------------------------------------------

    private static string SheetXml(ExcelSheet sheet)
    {
        var columnCount = sheet.Columns.Count;
        var lastColumn = ColumnLetter(columnCount);
        var lastRow = sheet.Rows.Count + 1;

        var builder = new StringBuilder();
        builder.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        builder.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");
        builder.Append($"""<dimension ref="A1:{lastColumn}{lastRow}"/>""");
        builder.Append("""<sheetViews><sheetView workbookViewId="0"><pane ySplit="1" topLeftCell="A2" activePane="bottomLeft" state="frozen"/></sheetView></sheetViews>""");
        builder.Append("""<sheetFormatPr defaultRowHeight="15"/>""");

        builder.Append("<cols>");
        for (var c = 0; c < columnCount; c++)
        {
            var width = sheet.Columns[c].Width ?? EstimateWidth(sheet, c);
            builder.Append($"""<col min="{c + 1}" max="{c + 1}" width="{width.ToString("0.##", CultureInfo.InvariantCulture)}" customWidth="1"/>""");
        }
        builder.Append("</cols>");

        builder.Append("<sheetData>");

        builder.Append("""<row r="1">""");
        for (var c = 0; c < columnCount; c++)
        {
            AppendInlineString(builder, $"{ColumnLetter(c + 1)}1", StyleHeader, sheet.Columns[c].Header);
        }
        builder.Append("</row>");

        for (var r = 0; r < sheet.Rows.Count; r++)
        {
            var rowRef = r + 2;
            builder.Append($"""<row r="{rowRef}">""");
            var cells = sheet.Rows[r];
            for (var c = 0; c < columnCount && c < cells.Length; c++)
            {
                AppendCell(builder, $"{ColumnLetter(c + 1)}{rowRef}", sheet.Columns[c].Format, cells[c]);
            }
            builder.Append("</row>");
        }

        builder.Append("</sheetData>");
        builder.Append($"""<autoFilter ref="A1:{lastColumn}{lastRow}"/>""");
        builder.Append("</worksheet>");
        return builder.ToString();
    }

    private static void AppendCell(StringBuilder builder, string cellRef, ExcelFormat format, object? value)
    {
        if (value is null)
        {
            return;
        }

        var style = StyleFor(format);
        switch (value)
        {
            case DateTimeOffset dto:
                AppendNumber(builder, cellRef, DateStyle(format), dto.DateTime.ToOADate());
                break;
            case DateTime dt:
                AppendNumber(builder, cellRef, DateStyle(format), dt.ToOADate());
                break;
            case DateOnly d:
                AppendNumber(builder, cellRef, DateStyle(format), d.ToDateTime(TimeOnly.MinValue).ToOADate());
                break;
            case bool b:
                AppendInlineString(builder, cellRef, style, b ? "Yes" : "No");
                break;
            case decimal or double or float or int or long or short or byte or uint or ulong or ushort or sbyte:
                AppendNumber(builder, cellRef, style, Convert.ToDouble(value, CultureInfo.InvariantCulture));
                break;
            default:
                AppendInlineString(builder, cellRef, StyleDefault, value.ToString() ?? "");
                break;
        }
    }

    /// <summary>Dates always take a date style so a mistyped column format still yields a readable cell.</summary>
    private static int DateStyle(ExcelFormat format) =>
        format == ExcelFormat.DateTime ? StyleFor(ExcelFormat.DateTime) : StyleFor(ExcelFormat.Date);

    private static void AppendNumber(StringBuilder builder, string cellRef, int style, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return;
        }
        builder.Append($"""<c r="{cellRef}" s="{style}"><v>{value.ToString("R", CultureInfo.InvariantCulture)}</v></c>""");
    }

    private static void AppendInlineString(StringBuilder builder, string cellRef, int style, string value)
    {
        if (value.Length == 0)
        {
            builder.Append($"""<c r="{cellRef}" s="{style}"/>""");
            return;
        }
        // preserve leading/trailing whitespace per the OOXML spec
        var space = value[0] == ' ' || value[^1] == ' ' ? " xml:space=\"preserve\"" : "";
        builder.Append($"""<c r="{cellRef}" s="{style}" t="inlineStr"><is><t{space}>{Escape(value)}</t></is></c>""");
    }

    // ----- helpers --------------------------------------------------------

    private static double EstimateWidth(ExcelSheet sheet, int columnIndex)
    {
        var longest = sheet.Columns[columnIndex].Header.Length;
        var sampled = 0;
        foreach (var row in sheet.Rows)
        {
            if (sampled++ >= 100) break;
            if (columnIndex >= row.Length) continue;
            var length = CellTextLength(row[columnIndex]);
            if (length > longest) longest = length;
        }
        // +3 leaves room for the autofilter dropdown on the header
        return Math.Clamp(longest + 3, 10, 60);
    }

    private static int CellTextLength(object? value) => value switch
    {
        null => 0,
        DateTimeOffset or DateTime or DateOnly => 10,
        decimal m => m.ToString("#,##0.00", CultureInfo.InvariantCulture).Length + 1,
        double d => d.ToString("#,##0.00", CultureInfo.InvariantCulture).Length + 1,
        _ => (value.ToString() ?? "").Length,
    };

    internal static string ColumnLetter(int columnNumber)
    {
        var letters = "";
        while (columnNumber > 0)
        {
            columnNumber--;
            letters = (char)('A' + columnNumber % 26) + letters;
            columnNumber /= 26;
        }
        return letters;
    }

    private static string SanitizeSheetName(string name, int index, HashSet<string> used)
    {
        var cleaned = new string(name.Where(ch => ch is not ('[' or ']' or ':' or '*' or '?' or '/' or '\\')).ToArray()).Trim('\'', ' ');
        if (cleaned.Length == 0) cleaned = $"Sheet{index + 1}";
        if (cleaned.Length > 31) cleaned = cleaned[..31];
        var candidate = cleaned;
        var suffix = 2;
        while (!used.Add(candidate))
        {
            var tail = $" ({suffix++})";
            candidate = cleaned.Length + tail.Length > 31 ? cleaned[..(31 - tail.Length)] + tail : cleaned + tail;
        }
        return candidate;
    }

    private static string Escape(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '&': builder.Append("&amp;"); break;
                case '<': builder.Append("&lt;"); break;
                case '>': builder.Append("&gt;"); break;
                case '"': builder.Append("&quot;"); break;
                case '\t' or '\n' or '\r': builder.Append(ch); break;
                default:
                    if (ch < 0x20 || ch == 0xFFFE || ch == 0xFFFF) break; // drop control chars Excel rejects
                    builder.Append(ch);
                    break;
            }
        }
        return builder.ToString();
    }
}
