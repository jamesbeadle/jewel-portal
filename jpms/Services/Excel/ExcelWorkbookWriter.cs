using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Jewel.JPMS.Services.Excel;

/// <summary>
/// Writes an <see cref="ExcelWorkbook"/> as a real .xlsx file (SpreadsheetML in a zip)
/// with no external dependencies, keeping the WASM payload small. Data sheets get a
/// bold frozen header row, an autofilter, sensible column widths, and per-column
/// number formats; presentation sheets (built from <see cref="ExcelStyledCell"/>s)
/// additionally get fills, fonts, borders, merges and print setup from a style
/// registry that grows only with the styles actually used.
/// </summary>
public static class ExcelWorkbookWriter
{
    public static byte[] Write(ExcelWorkbook workbook)
    {
        if (workbook.Sheets.Count == 0)
        {
            throw new InvalidOperationException("Cannot export a workbook with no sheets.");
        }

        // Sheets render FIRST so the style registry has seen every styled cell by the
        // time styles.xml is written; entry order inside the zip is irrelevant to Excel.
        var styles = new StyleRegistry();
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

    // ----- styles ---------------------------------------------------------

    /// <summary>
    /// Builds styles.xml from the styles the sheets actually reference. The first eight
    /// cellXfs reproduce the writer's classic fixed indexes (default, header, and the six
    /// column formats) so plain data sheets keep rendering exactly as before; presentation
    /// styles are registered on first use and deduplicated by value.
    /// </summary>
    private sealed class StyleRegistry
    {
        // JewelBB document palette — matches the branded PDF renderers.
        private const string MutedArgb = "FF606672";
        private const string NavyArgb = "FF1A1E29";
        private const string GoldArgb = "FFC09A51";
        private const string WhiteArgb = "FFFFFFFF";
        private const string NegativeArgb = "FFB42318";
        private const string PanelArgb = "FFF3F3F5";
        private const string HighlightArgb = "FFFBF2E2";
        private const string LegacyHeaderFillArgb = "FFF2F1EE";
        private const string HairlineArgb = "FFB9B6B0";
        private const string AccentArgb = "FFFF8300";

        private sealed record FontSpec(double Size, bool Bold, string? ColorArgb);
        private sealed record XfKey(int FontId, int FillId, int BorderId, int NumFmtId, ExcelAlign Align, bool WrapText);

        private readonly List<FontSpec> fonts = new();
        private readonly List<string?> fills = new();     // solid fill ARGB; null = none, "gray125" = the mandatory second fill
        private readonly List<ExcelBorder> borders = new();
        private readonly List<XfKey> cellXfs = new();
        private readonly Dictionary<XfKey, int> xfIndex = new();

        public StyleRegistry()
        {
            FontId(new FontSpec(11, false, null));                       // font 0 — default
            FontId(new FontSpec(11, true, null));                        // font 1 — bold
            fills.Add(null); fills.Add("gray125");                       // fills 0, 1 — required by the spec
            borders.Add(ExcelBorder.None);                               // border 0

            // The eight classic cellXfs, in their historical order: 0 default, 1 header,
            // 2 integer, 3 number, 4 currency, 5 date, 6 datetime, 7 percent.
            Register(new XfKey(0, 0, 0, 0, ExcelAlign.Auto, false));
            Register(new XfKey(1, FillId(LegacyHeaderFillArgb), BorderId(ExcelBorder.Hairline), 0, ExcelAlign.Auto, false));
            Register(new XfKey(0, 0, 0, NumFmtId(ExcelFormat.Integer), ExcelAlign.Auto, false));
            Register(new XfKey(0, 0, 0, NumFmtId(ExcelFormat.Number), ExcelAlign.Auto, false));
            Register(new XfKey(0, 0, 0, NumFmtId(ExcelFormat.Currency), ExcelAlign.Auto, false));
            Register(new XfKey(0, 0, 0, NumFmtId(ExcelFormat.Date), ExcelAlign.Auto, false));
            Register(new XfKey(0, 0, 0, NumFmtId(ExcelFormat.DateTime), ExcelAlign.Auto, false));
            Register(new XfKey(0, 0, 0, NumFmtId(ExcelFormat.Percent), ExcelAlign.Auto, false));
        }

        public int Header => 1;

        public int For(ExcelFormat format) => format switch
        {
            ExcelFormat.Integer => 2,
            ExcelFormat.Number => 3,
            ExcelFormat.Currency => 4,
            ExcelFormat.Date => 5,
            ExcelFormat.DateTime => 6,
            ExcelFormat.Percent => 7,
            _ => 0,
        };

        public int For(ExcelCellStyle style)
        {
            var font = style.Font switch
            {
                ExcelFont.Bold => new FontSpec(11, true, null),
                ExcelFont.Muted => new FontSpec(10, false, MutedArgb),
                ExcelFont.SmallMuted => new FontSpec(9, false, MutedArgb),
                ExcelFont.Title => new FontSpec(16, true, WhiteArgb),
                ExcelFont.Gold => new FontSpec(10, true, GoldArgb),
                ExcelFont.BandText => new FontSpec(9, false, WhiteArgb),
                ExcelFont.NavyBold => new FontSpec(11, true, NavyArgb),
                ExcelFont.Negative => new FontSpec(11, false, NegativeArgb),
                _ => new FontSpec(11, false, null),
            };
            var fill = style.Fill switch
            {
                ExcelFill.Navy => FillId(NavyArgb),
                ExcelFill.Panel => FillId(PanelArgb),
                ExcelFill.Highlight => FillId(HighlightArgb),
                _ => 0,
            };
            return Register(new XfKey(
                FontId(font), fill, BorderId(style.Border), NumFmtId(style.Format), style.Align, style.WrapText));
        }

        private int FontId(FontSpec spec)
        {
            var index = fonts.IndexOf(spec);
            if (index >= 0) return index;
            fonts.Add(spec);
            return fonts.Count - 1;
        }

        private int FillId(string argb)
        {
            var index = fills.IndexOf(argb);
            if (index >= 0) return index;
            fills.Add(argb);
            return fills.Count - 1;
        }

        private int BorderId(ExcelBorder border)
        {
            var index = borders.IndexOf(border);
            if (index >= 0) return index;
            borders.Add(border);
            return borders.Count - 1;
        }

        private static int NumFmtId(ExcelFormat format) => format switch
        {
            ExcelFormat.Integer => 164,
            ExcelFormat.Number => 165,
            ExcelFormat.Currency => 166,
            ExcelFormat.Date => 167,
            ExcelFormat.DateTime => 168,
            ExcelFormat.Percent => 169,
            _ => 0,
        };

        private int Register(XfKey key)
        {
            if (xfIndex.TryGetValue(key, out var existing)) return existing;
            cellXfs.Add(key);
            var index = cellXfs.Count - 1;
            xfIndex[key] = index;
            return index;
        }

        public string ToXml()
        {
            var builder = new StringBuilder();
            builder.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
            builder.Append("""<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");

            builder.Append("""<numFmts count="6">""");
            builder.Append("""<numFmt numFmtId="164" formatCode="#,##0"/>""");
            builder.Append("""<numFmt numFmtId="165" formatCode="#,##0.00"/>""");
            builder.Append("""<numFmt numFmtId="166" formatCode="&quot;£&quot;#,##0.00"/>""");
            builder.Append("""<numFmt numFmtId="167" formatCode="dd/mm/yyyy"/>""");
            builder.Append("""<numFmt numFmtId="168" formatCode="dd/mm/yyyy\ hh:mm"/>""");
            builder.Append("""<numFmt numFmtId="169" formatCode="0.0%"/>""");
            builder.Append("</numFmts>");

            builder.Append($"""<fonts count="{fonts.Count}">""");
            foreach (var font in fonts)
            {
                builder.Append("<font>");
                if (font.Bold) builder.Append("<b/>");
                builder.Append($"""<sz val="{font.Size.ToString("0.##", CultureInfo.InvariantCulture)}"/>""");
                if (font.ColorArgb is not null) builder.Append($"""<color rgb="{font.ColorArgb}"/>""");
                builder.Append("""<name val="Calibri"/></font>""");
            }
            builder.Append("</fonts>");

            builder.Append($"""<fills count="{fills.Count}">""");
            foreach (var fill in fills)
            {
                builder.Append(fill switch
                {
                    null => """<fill><patternFill patternType="none"/></fill>""",
                    "gray125" => """<fill><patternFill patternType="gray125"/></fill>""",
                    _ => $"""<fill><patternFill patternType="solid"><fgColor rgb="{fill}"/></patternFill></fill>""",
                });
            }
            builder.Append("</fills>");

            builder.Append($"""<borders count="{borders.Count}">""");
            foreach (var border in borders)
            {
                builder.Append(border switch
                {
                    ExcelBorder.Hairline => $"""<border><left/><right/><top/><bottom style="thin"><color rgb="{HairlineArgb}"/></bottom><diagonal/></border>""",
                    ExcelBorder.Accent => $"""<border><left/><right/><top/><bottom style="medium"><color rgb="{AccentArgb}"/></bottom><diagonal/></border>""",
                    _ => "<border><left/><right/><top/><bottom/><diagonal/></border>",
                });
            }
            builder.Append("</borders>");

            builder.Append("""<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>""");

            builder.Append($"""<cellXfs count="{cellXfs.Count}">""");
            foreach (var xf in cellXfs)
            {
                builder.Append($"<xf numFmtId=\"{xf.NumFmtId}\" fontId=\"{xf.FontId}\" fillId=\"{xf.FillId}\" borderId=\"{xf.BorderId}\" xfId=\"0\"");
                if (xf.NumFmtId != 0) builder.Append(" applyNumberFormat=\"1\"");
                if (xf.FontId != 0) builder.Append(" applyFont=\"1\"");
                if (xf.FillId != 0) builder.Append(" applyFill=\"1\"");
                if (xf.BorderId != 0) builder.Append(" applyBorder=\"1\"");
                if (xf.Align != ExcelAlign.Auto || xf.WrapText)
                {
                    builder.Append(" applyAlignment=\"1\"><alignment");
                    if (xf.Align != ExcelAlign.Auto)
                        builder.Append($" horizontal=\"{xf.Align.ToString().ToLowerInvariant()}\"");
                    if (xf.WrapText) builder.Append(" wrapText=\"1\" vertical=\"top\"");
                    builder.Append("/></xf>");
                }
                else
                {
                    builder.Append("/>");
                }
            }
            builder.Append("</cellXfs>");

            builder.Append("""<cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>""");
            builder.Append("</styleSheet>");
            return builder.ToString();
        }
    }

    // ----- worksheet ------------------------------------------------------

    private static string SheetXml(ExcelSheet sheet, StyleRegistry styles)
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
        if (sheet.ShowHeaderRow && sheet.FreezeHeaderRow)
        {
            builder.Append("""><pane ySplit="1" topLeftCell="A2" activePane="bottomLeft" state="frozen"/></sheetView></sheetViews>""");
        }
        else
        {
            builder.Append("/></sheetViews>");
        }
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

    private static void AppendCell(StringBuilder builder, string cellRef, ExcelFormat columnFormat, object? value, StyleRegistry styles)
    {
        if (value is ExcelStyledCell styled)
        {
            var styleId = styles.For(styled.Style);
            // A styled null still renders — that's how band fills and spacer cells exist.
            if (styled.Value is null)
            {
                builder.Append($"""<c r="{cellRef}" s="{styleId}"/>""");
                return;
            }
            AppendValue(builder, cellRef, styleId, styled.Value, styles, styled.Style.Format);
            return;
        }

        if (value is null)
        {
            return;
        }

        AppendValue(builder, cellRef, styles.For(columnFormat), value, styles, columnFormat);
    }

    private static void AppendValue(StringBuilder builder, string cellRef, int styleId, object value, StyleRegistry styles, ExcelFormat format)
    {
        switch (value)
        {
            case DateTimeOffset dto:
                AppendNumber(builder, cellRef, DateStyle(styleId, format, styles), dto.DateTime.ToOADate());
                break;
            case DateTime dt:
                AppendNumber(builder, cellRef, DateStyle(styleId, format, styles), dt.ToOADate());
                break;
            case DateOnly d:
                AppendNumber(builder, cellRef, DateStyle(styleId, format, styles), d.ToDateTime(TimeOnly.MinValue).ToOADate());
                break;
            case bool b:
                AppendInlineString(builder, cellRef, styleId, b ? "Yes" : "No");
                break;
            case decimal or double or float or int or long or short or byte or uint or ulong or ushort or sbyte:
                AppendNumber(builder, cellRef, styleId, Convert.ToDouble(value, CultureInfo.InvariantCulture));
                break;
            default:
                AppendInlineString(builder, cellRef, styleId, value.ToString() ?? "");
                break;
        }
    }

    /// <summary>Dates always take a date style so a mistyped column format still yields a readable cell.</summary>
    private static int DateStyle(int styleId, ExcelFormat format, StyleRegistry styles) =>
        format is ExcelFormat.Date or ExcelFormat.DateTime
            ? styleId
            : styles.For(ExcelFormat.Date);

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
        ExcelStyledCell styled => CellTextLength(styled.Value),
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
