using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using ClosedXML.Excel;
using Ganss.Xss;
using Jewel.JPMS.Api.Features.Agents;

namespace Jewel.JPMS.Api.Features.Ai.Sources;

/// <summary>
/// The ONE place a file becomes something the assistant can read — whatever medium it came from.
/// <see cref="Load"/> opens the bytes into an <see cref="AiSourceDocument"/> of parts and units;
/// <see cref="Read"/> pages through it from any position under a character budget; <see cref="Search"/>
/// finds where a reference appears. Spreadsheets become one part per sheet (displayed values,
/// tab-separated, every row from 1 to the last used row so unit numbers ARE row numbers), PDFs one
/// part per page (reading order, PdfPig), Word documents one body part of paragraphs and table
/// rows (the docx zip read directly), text files one part of lines (BOM-aware; HTML flattened),
/// images one part carrying the bytes the model is shown. What genuinely cannot be read — a
/// password-protected PDF, a scan with no text layer, a legacy .doc/.xls — throws the honest
/// sentence rather than returning half a document.
///
/// <para>Replaces the extract-once-and-cap approach (AiAttachmentReader.Extract, which now
/// delegates here): a 25,000-character cap over a whole workbook meant the first sheet ate the
/// budget and every later tab was silently never extracted — the V01 failure of 2026-08-25. Now
/// no file is unreadable, only long, and the model reads the part it needs.</para>
/// </summary>
internal static class AiSourceReader
{
    /// <summary>Per-call ceiling on a read: the result replays in the transcript (latest copy
    /// only) and one part is normally far smaller than this.</summary>
    public const int DefaultReadChars = 20_000;
    public const int MinReadChars = 2_000;
    public const int MaxReadChars = 50_000;

    /// <summary>How much of the first part rides on the Context row as a preview — enough to see
    /// what a file is, small enough to replay every hop for nothing.</summary>
    public const int PreviewChars = 2_000;

    private static readonly string[] TextExtensions =
        { ".txt", ".csv", ".tsv", ".md", ".json", ".xml", ".htm", ".html", ".eml", ".log" };

    private static readonly string[] SpreadsheetExtensions = { ".xlsx", ".xlsm" };

    public static bool IsSupported(string fileName, string? contentType = null) =>
        AiAttachmentReader.IsImage(fileName)
        || IsSpreadsheet(fileName, contentType)
        || IsTextLike(fileName, contentType)
        || Path.GetExtension(fileName).ToLowerInvariant() is ".pdf" or ".docx";

    // ---- Load -------------------------------------------------------------------------------

    /// <summary>Opens the bytes as the document they are, routed on extension with the wire
    /// content type as a second opinion (an email's "spreadsheetml" attachment with an odd name
    /// still opens as a workbook). Throws <see cref="NotSupportedException"/> for a format outside
    /// the list and <see cref="InvalidDataException"/> when the file cannot be read as what it
    /// claims to be.</summary>
    public static AiSourceDocument Load(string fileName, string? contentType, byte[] content)
    {
        if (AiAttachmentReader.EmailImageMediaType(fileName, contentType) is { } imageMediaType)
        {
            if (!AiAttachmentReader.LooksLike(imageMediaType, content))
                throw new InvalidDataException($"\"{fileName}\" does not look like a real {imageMediaType} image.");
            return new AiSourceDocument(AiSourceDocument.Image,
                new[] { new AiSourcePart("image", Path.GetFileName(fileName), "image", Array.Empty<string>()) },
                imageMediaType, content);
        }

        if (IsSpreadsheet(fileName, contentType)) return LoadWorkbook(content);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension == ".pdf" || contentType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) == true)
            return LoadPdf(content);
        if (extension == ".docx" || contentType?.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase) == true)
            return LoadDocx(content);
        if (IsTextLike(fileName, contentType))
            return LoadText(content, LooksLikeHtml(fileName, contentType));

        throw new NotSupportedException(
            $"\"{fileName}\" is not a format the assistant can read yet — attach {AiAttachmentReader.SupportedList}.");
    }

    private static AiSourceDocument LoadWorkbook(byte[] content)
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(new MemoryStream(content));
        }
        catch (Exception ex)
        {
            throw new InvalidDataException(
                "That file could not be opened as a spreadsheet — if it is an old .xls, save it as .xlsx first.", ex);
        }

        using (workbook)
        {
            var parts = new List<AiSourcePart>();
            foreach (var sheet in workbook.Worksheets)
            {
                // Contents only — values and formulas. The default (AllContents) counts data
                // validation, conditional formats and merged ranges, so a whole-column dropdown
                // (D:D) reports the last row as 1,048,576 and the loop below would build a
                // million empty rows at upload and on every read.
                var lastRow = sheet.LastRowUsed(XLCellsUsedOptions.Contents);
                var lastColumn = sheet.LastColumnUsed(XLCellsUsedOptions.Contents);
                if (lastRow is null || lastColumn is null) continue;

                var lastRowNumber = lastRow.RowNumber();
                var lastColumnNumber = lastColumn.ColumnNumber();
                var rows = new List<string>(lastRowNumber);
                for (var rowNumber = 1; rowNumber <= lastRowNumber; rowNumber++)
                {
                    var row = sheet.Row(rowNumber);
                    var cells = new string[lastColumnNumber];
                    for (var columnNumber = 1; columnNumber <= lastColumnNumber; columnNumber++)
                    {
                        // GetFormattedString so dates read as dates and money as money — the
                        // DISPLAYED value is what the boss meant, not the raw serial behind it.
                        // Every column from 1 to the last used one, blanks included, so the
                        // columns stay aligned across rows (the old CellsUsed join collapsed
                        // them and a price could slide under the wrong heading).
                        cells[columnNumber - 1] = row.Cell(columnNumber).GetFormattedString().Trim();
                    }
                    rows.Add(string.Join('\t', cells).TrimEnd('\t'));
                }
                parts.Add(new AiSourcePart(sheet.Name, sheet.Name, "row", rows));
            }

            if (parts.Count == 0)
                throw new InvalidDataException("That spreadsheet has no readable content — every sheet is empty.");

            return new AiSourceDocument(AiSourceDocument.Workbook, parts);
        }
    }

    private static AiSourceDocument LoadPdf(byte[] content)
    {
        UglyToad.PdfPig.PdfDocument document;
        try
        {
            document = UglyToad.PdfPig.PdfDocument.Open(content);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException(
                "That PDF could not be opened — it may be password-protected or corrupted.", ex);
        }

        using (document)
        {
            var parts = new List<AiSourcePart>();
            var anyText = false;
            foreach (var page in document.GetPages())
            {
                string pageText;
                try
                {
                    // Reading order beats raw content-stream order — a two-column spec sheet
                    // read stream-wise interleaves the columns into nonsense.
                    pageText = UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor
                        .ContentOrderTextExtractor.GetText(page);
                }
                catch (Exception)
                {
                    pageText = page.Text;
                }

                var lines = SplitLines(pageText);
                if (lines.Count > 0) anyText = true;
                parts.Add(new AiSourcePart($"p{page.Number}", $"Page {page.Number}", "line", lines));
            }

            if (!anyText)
            {
                throw new InvalidDataException(
                    "That PDF has no selectable text — it is likely a scan. Reading scans needs "
                    + "OCR, which is not available here; the figures have to come from the user.");
            }

            return new AiSourceDocument(AiSourceDocument.Pdf, parts);
        }
    }

    private static readonly XNamespace WordNs =
        "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    private static AiSourceDocument LoadDocx(byte[] content)
    {
        try
        {
            using var stream = new MemoryStream(content);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            var entry = zip.GetEntry("word/document.xml")
                ?? throw new InvalidDataException("no word/document.xml inside the file");

            XDocument xml;
            using (var entryStream = entry.Open()) xml = XDocument.Load(entryStream);
            var body = xml.Root?.Element(WordNs + "body")
                ?? throw new InvalidDataException("the document has no body");

            var units = new List<string>();
            AppendDocxBlocks(body, units);

            if (units.All(string.IsNullOrWhiteSpace))
                throw new InvalidDataException("the document has no readable text");

            return new AiSourceDocument(AiSourceDocument.WordDocument,
                new[] { new AiSourcePart("body", "Document", "paragraph", units) });
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidDataException(
                "That file could not be opened as a Word document — if it is an old .doc, save it as .docx first.", ex);
        }
    }

    private static void AppendDocxBlocks(XElement container, List<string> units)
    {
        foreach (var element in container.Elements())
        {
            if (element.Name == WordNs + "p")
            {
                units.Add(DocxParagraphText(element));
            }
            else if (element.Name == WordNs + "tbl")
            {
                foreach (var row in element.Elements(WordNs + "tr"))
                    units.Add(string.Join('\t', row.Elements(WordNs + "tc").Select(DocxCellText)));
            }
            else if (element.Name == WordNs + "sdt")
            {
                // A content control wraps real content — unwrap it.
                if (element.Element(WordNs + "sdtContent") is { } inner)
                    AppendDocxBlocks(inner, units);
            }
        }
    }

    private static string DocxParagraphText(XElement paragraph) =>
        string.Concat(paragraph.Descendants().Select(node =>
            node.Name == WordNs + "t" ? node.Value
            : node.Name == WordNs + "tab" ? "\t"
            : node.Name == WordNs + "br" || node.Name == WordNs + "cr" ? "\n"
            : ""));

    private static string DocxCellText(XElement cell) =>
        string.Join(" ", cell.Elements(WordNs + "p")
            .Select(DocxParagraphText)
            .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph)));

    private static AiSourceDocument LoadText(byte[] content, bool isHtml)
    {
        string text;
        try
        {
            // BOM-aware: Excel exports CSVs as UTF-16 (and UTF-8 with a BOM) often enough that
            // blind UTF-8 turned them to spaced-out mojibake. No BOM falls back to UTF-8.
            using var stream = new MemoryStream(content);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            text = reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new InvalidDataException("That file could not be read as text.", ex);
        }

        if (isHtml)
            text = RequestContextAssembler.HtmlToText(new HtmlSanitizer().Sanitize(text));

        var lines = SplitLines(text);
        if (lines.Count == 0)
            throw new InvalidDataException("That file has no readable content.");

        return new AiSourceDocument(AiSourceDocument.Text,
            new[] { new AiSourcePart("text", "Text", "line", lines) });
    }

    private static List<string> SplitLines(string text)
    {
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
            .Select(line => line.TrimEnd())
            .ToList();
        // Trailing blanks are nothing; interior blanks keep their place so line numbers hold.
        while (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
        while (lines.Count > 0 && lines[0].Length == 0) lines.RemoveAt(0);
        return lines;
    }

    // ---- Read -------------------------------------------------------------------------------

    /// <summary>
    /// Reads from a position under a character budget. With <paramref name="partKey"/> given the
    /// read stays inside that part (the model asked for the V01 tab, not the V01 tab and half of
    /// V02) and <c>Next</c> points at the following part when there is one; with it omitted the
    /// read starts at the first part and flows across part boundaries — each announced by a
    /// header line — until the budget is spent, so a twelve-page PDF reads in a handful of calls.
    /// At least one unit is always returned, whatever the budget.
    /// </summary>
    public static AiSourceReadResult Read(AiSourceDocument document, string? partKey, int from, int maxChars)
    {
        if (document.Parts.Count == 0)
            return new AiSourceReadResult("", "", "", 0, 0, true, null);

        var explicitPart = !string.IsNullOrWhiteSpace(partKey);
        var startIndex = 0;
        if (explicitPart)
        {
            var part = document.Part(partKey)
                ?? throw new ArgumentException($"No part named \"{partKey}\".", nameof(partKey));
            startIndex = document.Parts.ToList().IndexOf(part);
        }

        maxChars = Math.Clamp(maxChars, MinReadChars, MaxReadChars);
        var text = new StringBuilder();
        var startPart = document.Parts[startIndex];
        var fromUnit = Math.Max(1, from);
        var toUnit = fromUnit - 1;
        AiSourcePosition? next = null;
        var reachedEnd = false;
        var anyUnit = false;

        for (var partIndex = startIndex; partIndex < document.Parts.Count; partIndex++)
        {
            var part = document.Parts[partIndex];
            var first = partIndex == startIndex ? fromUnit : 1;

            if (first > part.Units.Count && partIndex == startIndex && part.Units.Count > 0)
            {
                // Asked to start past the end of the part: nothing here, say so honestly.
                return new AiSourceReadResult(
                    $"[{Header(document, part)} has {part.Units.Count} {part.UnitName}s — nothing from {first} onwards.]",
                    part.Key, part.Label, first, part.Units.Count, true,
                    partIndex + 1 < document.Parts.Count ? new AiSourcePosition(document.Parts[partIndex + 1].Key, 1) : null);
            }

            if (text.Length > 0) text.AppendLine();
            text.AppendLine($"[{Header(document, part)}{(first > 1 ? $" — from {part.UnitName} {first}" : "")}]");

            var stoppedInside = false;
            for (var unit = first; unit <= part.Units.Count; unit++)
            {
                var line = FormatUnit(document, part, unit);
                if (anyUnit && text.Length + line.Length + 1 > maxChars)
                {
                    next = new AiSourcePosition(part.Key, unit);
                    stoppedInside = true;
                    break;
                }
                text.AppendLine(line);
                anyUnit = true;
                toUnit = unit;
            }

            if (stoppedInside)
            {
                text.AppendLine($"[… continues at {part.UnitName} {next!.From} of {Header(document, part)} — call read_source again with part \"{part.Key}\" and from {next.From}.]");
                return new AiSourceReadResult(text.ToString().TrimEnd(), startPart.Key, startPart.Label, fromUnit, toUnit, false, next);
            }

            // The part is finished. Stop here when the caller named it; otherwise flow on.
            var hasMoreParts = partIndex + 1 < document.Parts.Count;
            if (explicitPart || !hasMoreParts)
            {
                reachedEnd = true;
                next = hasMoreParts ? new AiSourcePosition(document.Parts[partIndex + 1].Key, 1) : null;
                if (hasMoreParts)
                    text.AppendLine($"[End of {Header(document, part)}. Next part: «{document.Parts[partIndex + 1].Label}».]");
                return new AiSourceReadResult(text.ToString().TrimEnd(), startPart.Key, startPart.Label, fromUnit, toUnit, reachedEnd, next);
            }
        }

        return new AiSourceReadResult(text.ToString().TrimEnd(), startPart.Key, startPart.Label, fromUnit, toUnit, true, null);
    }

    /// <summary>The opening of the first part, for the Context row: what the file is at a glance.</summary>
    public static string Preview(AiSourceDocument document, int maxChars = PreviewChars)
    {
        if (document.IsImage || document.Parts.Count == 0) return "";
        var first = document.Parts[0];
        var text = new StringBuilder();
        text.AppendLine($"[{Header(document, first)} — opening {first.UnitName}s]");
        for (var unit = 1; unit <= first.Units.Count; unit++)
        {
            var line = FormatUnit(document, first, unit);
            if (text.Length + line.Length + 1 > maxChars) break;
            text.AppendLine(line);
        }
        return text.ToString().TrimEnd();
    }

    private static string Header(AiSourceDocument document, AiSourcePart part) => document.Kind switch
    {
        AiSourceDocument.Workbook => $"Sheet: {part.Label}",
        AiSourceDocument.Pdf => part.Label,
        _ => part.Label
    };

    /// <summary>Rows and lines carry their number so "row 12" can be quoted and paged to; a
    /// PDF's lines and a document's paragraphs read better bare (the page IS the unit people
    /// cite, and search hits still give the number).</summary>
    private static string FormatUnit(AiSourceDocument document, AiSourcePart part, int unit) =>
        document.Kind is AiSourceDocument.Workbook or AiSourceDocument.Text
            ? $"{unit}\t{part.Units[unit - 1]}"
            : part.Units[unit - 1];

    // ---- Search -----------------------------------------------------------------------------

    /// <summary>
    /// Where a query appears: unit hits (case-insensitive, whitespace-forgiving; a multi-word
    /// query that matches nothing as a phrase falls back to "every word present") and parts whose
    /// NAME matches — the sheet called "V01 - Levelling compound" is the answer to "V01" before
    /// any row is.
    /// </summary>
    public static AiSourceSearchResult Search(AiSourceDocument document, string query, int maxHits = 20)
    {
        var wanted = Normalise(query);
        if (wanted.Length == 0 || document.IsImage)
            return new AiSourceSearchResult(Array.Empty<AiSourceHit>(), Array.Empty<AiSourceManifestPart>(), 0);

        var partsByName = document.Parts
            .Where(part => Normalise(part.Label).Contains(wanted, StringComparison.OrdinalIgnoreCase)
                           || Normalise(part.Key).Contains(wanted, StringComparison.OrdinalIgnoreCase))
            .Select(part => new AiSourceManifestPart(part.Key, part.Label, part.UnitName, part.Units.Count, part.Chars))
            .ToList();

        var hits = new List<AiSourceHit>();
        var total = 0;
        Collect(document, unitText => Normalise(unitText).Contains(wanted, StringComparison.OrdinalIgnoreCase), hits, ref total, maxHits, wanted);

        if (total == 0)
        {
            var words = wanted.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1)
            {
                Collect(document,
                    unitText =>
                    {
                        var normalised = Normalise(unitText);
                        return words.All(word => normalised.Contains(word, StringComparison.OrdinalIgnoreCase));
                    },
                    hits, ref total, maxHits, words[0]);
            }
        }

        return new AiSourceSearchResult(hits, partsByName, total);
    }

    private static void Collect(
        AiSourceDocument document, Func<string, bool> matches, List<AiSourceHit> hits, ref int total, int maxHits, string anchor)
    {
        foreach (var part in document.Parts)
        {
            for (var unit = 1; unit <= part.Units.Count; unit++)
            {
                var text = part.Units[unit - 1];
                if (text.Length == 0 || !matches(text)) continue;
                total++;
                if (hits.Count < maxHits)
                    hits.Add(new AiSourceHit(part.Key, part.Label, unit, Snippet(text, anchor)));
            }
        }
    }

    /// <summary>Up to ~240 characters around the first occurrence — enough to read the row, not
    /// the whole 40-column line.</summary>
    private static string Snippet(string text, string anchor)
    {
        const int width = 240;
        if (text.Length <= width) return text;
        var at = text.IndexOf(anchor, StringComparison.OrdinalIgnoreCase);
        var start = Math.Max(0, Math.Min(at < 0 ? 0 : at - 60, text.Length - width));
        var piece = text.Substring(start, Math.Min(width, text.Length - start));
        return (start > 0 ? "…" : "") + piece + (start + piece.Length < text.Length ? "…" : "");
    }

    private static string Normalise(string value)
    {
        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;
        foreach (var character in value.Trim())
        {
            if (char.IsWhiteSpace(character)) { pendingSpace = true; continue; }
            if (pendingSpace && builder.Length > 0) builder.Append(' ');
            pendingSpace = false;
            builder.Append(character);
        }
        return builder.ToString();
    }

    // ---- Routing helpers (shared with the tools) --------------------------------------------

    public static bool IsSpreadsheet(string name, string? contentType)
    {
        if (contentType?.Contains("spreadsheetml", StringComparison.OrdinalIgnoreCase) == true) return true;
        return SpreadsheetExtensions.Contains(Path.GetExtension(name).ToLowerInvariant());
    }

    public static bool IsTextLike(string name, string? contentType)
    {
        if (contentType is not null)
        {
            var type = contentType.ToLowerInvariant();
            if (type.StartsWith("text/", StringComparison.Ordinal)) return true;
            if (type.Contains("json") || type.Contains("xml") || type.Contains("csv")) return true;
        }
        return TextExtensions.Contains(Path.GetExtension(name).ToLowerInvariant());
    }

    private static bool LooksLikeHtml(string name, string? contentType)
    {
        if (contentType?.Contains("html", StringComparison.OrdinalIgnoreCase) == true) return true;
        return Path.GetExtension(name).ToLowerInvariant() is ".htm" or ".html";
    }
}
