using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Jewel.JPMS.Api.Features.Procurement;

/// <summary>
/// Writes the pricing schedule workbook that travels with a tender invite — the sheet each
/// subcontractor fills in and returns. Modelled on the firm's hand-built tender sheets
/// (e.g. "Nicholas Nymet - Roofing Tender.xlsx", 2026-06): a title block with the specification
/// summary, then Cost code / Item / Description / Qty / Unit / Rate / Total rows grouped by trade,
/// with Total = Qty × Rate formulas and SUBTOTAL / NET / VAT / GROSS at the foot. Only the
/// subcontractor-facing columns are generated — Jewel's internal value/margin columns never leave
/// the office. Dependency-free SpreadsheetML-in-a-zip, the same approach as the client-side
/// ExcelWorkbookWriter (the API can't reference the WASM project, and the shape here — merges,
/// formulas, section rows — is this document's own).
/// </summary>
public static class PricingScheduleWorkbook
{
    /// <summary>One tenderable line: the cost-code / VO reference shown to the subcontractor, and
    /// the measured scope. Rate and Total are theirs to complete.</summary>
    public sealed record ScheduleLine(string Reference, string Description, decimal Quantity, string Unit);

    /// <summary>A trade section — the grouping the example sheets use ("High Level", "Roofing").</summary>
    public sealed record ScheduleSection(string Trade, IReadOnlyList<ScheduleLine> Lines);

    public static byte[] Write(
        string packageReference,
        string packageTitle,
        string projectName,
        string specificationSummary,
        bool materialsApplicable,
        IReadOnlyList<ScheduleSection> sections)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(zip, "[Content_Types].xml",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
                """<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">""" +
                """<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>""" +
                """<Default Extension="xml" ContentType="application/xml"/>""" +
                """<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>""" +
                """<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>""" +
                """<Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>""" +
                "</Types>");
            AddEntry(zip, "_rels/.rels",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
                """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""" +
                """<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>""" +
                "</Relationships>");
            AddEntry(zip, "xl/workbook.xml",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
                """<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">""" +
                """<sheets><sheet name="Pricing Schedule" sheetId="1" r:id="rId1"/></sheets>""" +
                "</workbook>");
            AddEntry(zip, "xl/_rels/workbook.xml.rels",
                """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
                """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""" +
                """<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>""" +
                "</Relationships>");
            AddEntry(zip, "xl/styles.xml", StylesXml());
            AddEntry(zip, "xl/worksheets/sheet1.xml",
                SheetXml(packageReference, packageTitle, projectName, specificationSummary, materialsApplicable, sections));
        }
        return stream.ToArray();
    }

    // ----- styles ----------------------------------------------------------------------------
    // Index map (cellXfs): 0 default · 1 bold · 2 header (bold, grey fill, border) · 3 wrapped
    // text · 4 currency (the subcontractor's Rate/Total cells) · 5 currency bold (totals) ·
    // 6 section row (bold, light fill) · 7 title (bold, larger) · 8 muted note text.
    private static string StylesXml() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
        """<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""" +
        """<numFmts count="1"><numFmt numFmtId="164" formatCode="#,##0.00"/></numFmts>""" +
        "<fonts count=\"5\">" +
        """<font><sz val="11"/><name val="Calibri"/></font>""" +
        """<font><b/><sz val="11"/><name val="Calibri"/></font>""" +
        """<font><b/><sz val="14"/><name val="Calibri"/></font>""" +
        """<font><i/><sz val="10"/><color rgb="FF666666"/><name val="Calibri"/></font>""" +
        """<font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font>""" +
        "</fonts>" +
        "<fills count=\"4\">" +
        """<fill><patternFill patternType="none"/></fill>""" +
        """<fill><patternFill patternType="gray125"/></fill>""" +
        """<fill><patternFill patternType="solid"><fgColor rgb="FF3F3F3F"/></patternFill></fill>""" +
        """<fill><patternFill patternType="solid"><fgColor rgb="FFEDEDED"/></patternFill></fill>""" +
        "</fills>" +
        "<borders count=\"2\">" +
        "<border><left/><right/><top/><bottom/><diagonal/></border>" +
        """<border><left style="thin"><color rgb="FFBFBFBF"/></left><right style="thin"><color rgb="FFBFBFBF"/></right><top style="thin"><color rgb="FFBFBFBF"/></top><bottom style="thin"><color rgb="FFBFBFBF"/></bottom><diagonal/></border>""" +
        "</borders>" +
        """<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>""" +
        "<cellXfs count=\"9\">" +
        """<xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>""" +
        """<xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0"/>""" +
        """<xf numFmtId="0" fontId="4" fillId="2" borderId="1" xfId="0" applyFill="1" applyBorder="1"/>""" +
        """<xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1" applyAlignment="1"><alignment wrapText="1" vertical="top"/></xf>""" +
        """<xf numFmtId="164" fontId="0" fillId="0" borderId="1" xfId="0" applyNumberFormat="1" applyBorder="1"/>""" +
        """<xf numFmtId="164" fontId="1" fillId="0" borderId="1" xfId="0" applyNumberFormat="1" applyBorder="1"/>""" +
        """<xf numFmtId="0" fontId="1" fillId="3" borderId="1" xfId="0" applyFill="1" applyBorder="1"/>""" +
        """<xf numFmtId="0" fontId="2" fillId="0" borderId="0" xfId="0"/>""" +
        """<xf numFmtId="0" fontId="3" fillId="0" borderId="0" xfId="0"/>""" +
        "</cellXfs>" +
        """<cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>""" +
        "</styleSheet>";

    // ----- the sheet ---------------------------------------------------------------------------

    private static string SheetXml(
        string packageReference, string packageTitle, string projectName,
        string specificationSummary, bool materialsApplicable,
        IReadOnlyList<ScheduleSection> sections)
    {
        var rows = new StringBuilder();
        var merges = new List<string>();
        var row = 0;

        void Merge(int r) => merges.Add($"A{r}:G{r}");

        void TextRow(string text, int style, bool merge = true)
        {
            row++;
            rows.Append($"<row r=\"{row}\">");
            rows.Append(TextCell($"A{row}", text, style));
            rows.Append("</row>");
            if (merge) Merge(row);
        }

        void BlankRow() { row++; rows.Append($"<row r=\"{row}\"/>"); }

        // ---- Title block ----
        TextRow("JEWEL BESPOKE BUILD — TENDER PRICING SCHEDULE", 7);
        TextRow($"{packageReference} — {packageTitle}", 1);
        if (!string.IsNullOrWhiteSpace(projectName)) TextRow(projectName, 0);
        BlankRow();

        if (!string.IsNullOrWhiteSpace(specificationSummary))
        {
            TextRow("SPECIFICATION SUMMARY", 1);
            foreach (var line in specificationSummary.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                TextRow($"• {line.Trim().TrimStart('•', '-', ' ')}", 0);
            BlankRow();
        }

        TextRow("Enter your rate against each line — totals calculate automatically. " +
                "Add any exclusions, assumptions or lead times when you return this schedule.", 8);
        if (materialsApplicable)
            TextRow("Materials apply to this scope: please state whether you will supply your own materials or have priced labour-only.", 8);
        BlankRow();

        // ---- Header ----
        row++;
        rows.Append($"<row r=\"{row}\">");
        var headers = new[] { "COST CODE / VO", "Item", "Description", "Qty", "Unit", "Rate (£)", "Total (£)" };
        for (var i = 0; i < headers.Length; i++)
            rows.Append(TextCell($"{Col(i)}{row}", headers[i], 2));
        rows.Append("</row>");
        var firstItemRow = row + 1;

        // ---- Sections and lines ----
        var itemRows = new List<int>();
        foreach (var section in sections)
        {
            if (sections.Count > 1 || !string.IsNullOrWhiteSpace(section.Trade))
            {
                row++;
                rows.Append($"<row r=\"{row}\">");
                rows.Append(TextCell($"A{row}", "", 6));
                rows.Append(TextCell($"B{row}", "", 6));
                rows.Append(TextCell($"C{row}", section.Trade, 6));
                for (var i = 3; i < 7; i++) rows.Append(TextCell($"{Col(i)}{row}", "", 6));
                rows.Append("</row>");
            }

            var item = 0;
            foreach (var line in section.Lines)
            {
                item++;
                row++;
                itemRows.Add(row);
                rows.Append($"<row r=\"{row}\">");
                rows.Append(TextCell($"A{row}", line.Reference, 3));
                rows.Append(NumberCell($"B{row}", item, 3));
                rows.Append(TextCell($"C{row}", line.Description, 3));
                rows.Append(NumberCell($"D{row}", line.Quantity, 3));
                rows.Append(TextCell($"E{row}", line.Unit, 3));
                rows.Append($"<c r=\"F{row}\" s=\"4\"/>");                                    // Rate — theirs to fill
                rows.Append($"<c r=\"G{row}\" s=\"4\"><f>D{row}*F{row}</f></c>");             // Total = Qty × Rate
                rows.Append("</row>");
            }
        }
        var lastItemRow = row;

        // ---- Footer ----
        BlankRow();
        row++;
        var subtotalRow = row;
        rows.Append($"<row r=\"{row}\">");
        rows.Append(TextCell($"C{row}", "SUBTOTAL — Measured Works", 1));
        rows.Append(itemRows.Count > 0
            ? $"<c r=\"G{row}\" s=\"5\"><f>SUM(G{firstItemRow}:G{lastItemRow})</f></c>"
            : $"<c r=\"G{row}\" s=\"5\"/>");
        rows.Append("</row>");

        row++;
        var netRow = row;
        rows.Append($"<row r=\"{row}\">");
        rows.Append(TextCell($"C{row}", "NET TOTAL (Excl. VAT)", 1));
        rows.Append($"<c r=\"G{row}\" s=\"5\"><f>G{subtotalRow}</f></c>");
        rows.Append("</row>");

        row++;
        rows.Append($"<row r=\"{row}\">");
        rows.Append(TextCell($"C{row}", "VAT @ 20%", 1));
        rows.Append($"<c r=\"G{row}\" s=\"5\"><f>G{netRow}*0.2</f></c>");
        rows.Append("</row>");

        row++;
        rows.Append($"<row r=\"{row}\">");
        rows.Append(TextCell($"C{row}", "GROSS TOTAL (Incl. VAT)", 1));
        rows.Append($"<c r=\"G{row}\" s=\"5\"><f>G{netRow}*1.2</f></c>");
        rows.Append("</row>");

        var builder = new StringBuilder();
        builder.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        builder.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");
        builder.Append("<cols>");
        builder.Append("""<col min="1" max="1" width="16" customWidth="1"/>""");   // Cost code / VO
        builder.Append("""<col min="2" max="2" width="6" customWidth="1"/>""");    // Item
        builder.Append("""<col min="3" max="3" width="64" customWidth="1"/>""");   // Description
        builder.Append("""<col min="4" max="4" width="9" customWidth="1"/>""");    // Qty
        builder.Append("""<col min="5" max="5" width="8" customWidth="1"/>""");    // Unit
        builder.Append("""<col min="6" max="6" width="12" customWidth="1"/>""");   // Rate
        builder.Append("""<col min="7" max="7" width="14" customWidth="1"/>""");   // Total
        builder.Append("</cols>");
        builder.Append("<sheetData>").Append(rows).Append("</sheetData>");
        if (merges.Count > 0)
        {
            builder.Append($"<mergeCells count=\"{merges.Count}\">");
            foreach (var merge in merges) builder.Append($"<mergeCell ref=\"{merge}\"/>");
            builder.Append("</mergeCells>");
        }
        builder.Append("</worksheet>");
        return builder.ToString();
    }

    // ----- cells -------------------------------------------------------------------------------

    private static string Col(int index) => ((char)('A' + index)).ToString();

    private static string TextCell(string reference, string value, int style) =>
        string.IsNullOrEmpty(value)
            ? $"<c r=\"{reference}\" s=\"{style}\"/>"
            : $"<c r=\"{reference}\" s=\"{style}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Escape(value)}</t></is></c>";

    private static string NumberCell(string reference, decimal value, int style) =>
        $"<c r=\"{reference}\" s=\"{style}\"><v>{value.ToString(CultureInfo.InvariantCulture)}</v></c>";

    private static string Escape(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
             .Replace("\"", "&quot;").Replace("\r", "").Replace("\n", " ");

    private static void AddEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }
}
