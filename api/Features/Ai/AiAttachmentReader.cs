using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using ClosedXML.Excel;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// Turns an attachment into text the model can read — the ONE extraction home, shared by the
/// chat's own uploads (extracted once at upload, persisted as a Context row, bytes discarded)
/// and read_email_attachment (extracted per call from the live Graph bytes). Spreadsheets come
/// back as displayed values row by row (ClosedXML), PDFs page by page (PdfPig), Word documents
/// paragraph by paragraph (the docx zip read directly — no extra dependency), text files as
/// text. Images are the one kind carried as BYTES, because the model reads them as image
/// blocks, not text. What genuinely cannot be read — a password-protected PDF, a scan with no
/// text layer, a legacy .doc/.xls — is refused with a plain sentence rather than half-read
/// (ADR-007: declared, not hidden).
/// </summary>
internal static class AiAttachmentReader
{
    /// <summary>Raw upload ceiling. A tracker workbook is tens of KB; this is generous headroom
    /// while staying far inside the gateway's request budget once base64-inflated.</summary>
    public const int MaxBytes = 10 * 1024 * 1024;

    /// <summary>Extracted-text ceiling. The text replays with EVERY hop of the conversation
    /// (that is the point — the model keeps the sheet in view), so it is capped hard rather than
    /// trusted to the transcript budget, which deliberately never touches non-tool rows.</summary>
    public const int MaxChars = 25_000;

    private static readonly string[] SupportedExtensions =
        { ".xlsx", ".csv", ".tsv", ".txt", ".pdf", ".docx" };

    /// <summary>
    /// Image formats reach the model as IMAGE blocks, not extracted text — the one attachment kind
    /// where the bytes themselves are what gets persisted (base64, on the Context row) and replayed.
    /// The list is exactly what the Anthropic Messages API accepts as image media types.
    /// </summary>
    private static readonly Dictionary<string, string> ImageMediaTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp"
        };

    public static bool IsSupported(string fileName) =>
        SupportedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant())
        || IsImage(fileName);

    public static bool IsImage(string fileName) =>
        ImageMediaTypes.ContainsKey(Path.GetExtension(fileName));

    public static string ImageMediaType(string fileName) =>
        ImageMediaTypes[Path.GetExtension(fileName)];

    public static string SupportedList =>
        "xlsx, csv, tsv, txt, pdf, docx — or an image (png, jpg, gif, webp)";

    /// <summary>
    /// Checks an image upload actually IS the image its name claims (magic bytes, not extension —
    /// a renamed .exe must not ride into the transcript as a "png") and returns the one-line human
    /// summary for the pill. Throws <see cref="InvalidDataException"/> when the bytes don't match.
    /// </summary>
    public static string ValidateImage(string fileName, byte[] content)
    {
        var mediaType = ImageMediaType(fileName);
        if (!LooksLike(mediaType, content))
            throw new InvalidDataException(
                $"\"{fileName}\" doesn't look like a real {Path.GetExtension(fileName).TrimStart('.')} image — re-save it and attach again.");

        return content.Length >= 1_048_576
            ? $"image · {content.Length / 1_048_576.0:0.#} MB"
            : $"image · {Math.Max(1, content.Length / 1024)} KB";
    }

    /// <summary>The magic-byte check itself, shared with read_email_attachment — an email image
    /// is validated by its bytes exactly like a pasted one.</summary>
    public static bool LooksLike(string mediaType, byte[] content) => mediaType switch
    {
        "image/png" => StartsWith(content, 0x89, 0x50, 0x4E, 0x47),
        "image/jpeg" => StartsWith(content, 0xFF, 0xD8, 0xFF),
        "image/gif" => StartsWith(content, 0x47, 0x49, 0x46, 0x38),
        "image/webp" => StartsWith(content, 0x52, 0x49, 0x46, 0x46)
                        && content.Length > 11
                        && content[8] == 0x57 && content[9] == 0x45
                        && content[10] == 0x42 && content[11] == 0x50,
        _ => false
    };

    /// <summary>The media type for an EMAIL attachment: the wire content type when it is one the
    /// model can be shown, else the file name's extension; null means "not a showable image"
    /// (svg included — it is markup, and falls through to the text path).</summary>
    public static string? EmailImageMediaType(string name, string? contentType)
    {
        var type = contentType?.ToLowerInvariant().Trim();
        if (type is "image/png" or "image/jpeg" or "image/gif" or "image/webp") return type;
        if (type is "image/jpg" or "image/pjpeg") return "image/jpeg";
        return ImageMediaTypes.TryGetValue(Path.GetExtension(name), out var byName) ? byName : null;
    }

    /// <summary>
    /// The image's longest side in pixels, read straight from the header (png, jpeg) — the
    /// Anthropic API rejects anything over 8,000px, and finding that out mid-turn wastes the
    /// hop. Null when the format's header is not sniffed (gif, webp) or unreadable.
    /// </summary>
    public static int? LongestSidePixels(string mediaType, byte[] content)
    {
        try
        {
            return mediaType switch
            {
                "image/png" when content.Length >= 24 =>
                    Math.Max(ReadBigEndian(content, 16), ReadBigEndian(content, 20)),
                "image/jpeg" => JpegLongestSide(content),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static int ReadBigEndian(byte[] content, int offset) =>
        (content[offset] << 24) | (content[offset + 1] << 16) | (content[offset + 2] << 8) | content[offset + 3];

    /// <summary>Walks the JPEG marker chain to the first frame header (SOFn) and reads its
    /// dimensions. Null when no frame header is found before the data ends.</summary>
    private static int? JpegLongestSide(byte[] content)
    {
        var index = 2;
        while (index + 9 < content.Length)
        {
            if (content[index] != 0xFF) { index++; continue; }
            var marker = content[index + 1];
            if (marker is 0xD8 or 0x01 or (>= 0xD0 and <= 0xD7)) { index += 2; continue; }
            var length = (content[index + 2] << 8) | content[index + 3];
            if (length < 2) return null;
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
            {
                var height = (content[index + 5] << 8) | content[index + 6];
                var width = (content[index + 7] << 8) | content[index + 8];
                return Math.Max(height, width);
            }
            index += 2 + length;
        }
        return null;
    }

    private static bool StartsWith(byte[] content, params byte[] signature)
    {
        if (content.Length < signature.Length) return false;
        for (var i = 0; i < signature.Length; i++)
            if (content[i] != signature[i]) return false;
        return true;
    }

    /// <summary>Extracts <paramref name="content"/> to prompt-ready text plus a one-line human
    /// summary. Throws <see cref="NotSupportedException"/> for a format outside the list and
    /// <see cref="InvalidDataException"/> when the file cannot be read as what it claims to be.</summary>
    public static (string Text, string Summary) Extract(string fileName, byte[] content)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".xlsx" => ExtractWorkbook(content),
            ".pdf" => ExtractPdf(content),
            ".docx" => ExtractDocx(content),
            ".csv" or ".tsv" or ".txt" => ExtractText(content),
            _ => throw new NotSupportedException(
                $"\"{fileName}\" is not a format the assistant can read yet — attach {SupportedList}.")
        };
    }

    private static (string, string) ExtractWorkbook(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var workbook = OpenWorkbook(stream);

        var text = new StringBuilder();
        var totalRows = 0;
        var sheets = 0;
        var truncated = false;

        foreach (var sheet in workbook.Worksheets)
        {
            var rows = sheet.RowsUsed().ToList();
            if (rows.Count == 0) continue;

            sheets++;
            if (text.Length > 0) text.AppendLine();
            text.AppendLine($"[Sheet: {sheet.Name}]");

            foreach (var row in rows)
            {
                if (text.Length >= MaxChars)
                {
                    truncated = true;
                    break;
                }
                totalRows++;
                // GetFormattedString so dates read as dates and money as money — the DISPLAYED
                // value is what the boss meant, not the raw serial behind it. Tabs between cells
                // keep a priced schedule readable as columns.
                text.AppendLine(string.Join('\t',
                    row.CellsUsed().Select(cell => cell.GetFormattedString().Trim())));
            }
            if (truncated) break;
        }

        if (truncated)
            text.AppendLine($"[… the workbook was larger than {MaxChars:N0} characters and has been cut here.]");

        return (text.ToString().TrimEnd(),
            $"{sheets} sheet{(sheets == 1 ? "" : "s")} · {totalRows} row{(totalRows == 1 ? "" : "s")}"
            + (truncated ? " (truncated)" : ""));
    }

    private static IXLWorkbook OpenWorkbook(Stream stream)
    {
        try
        {
            return new XLWorkbook(stream);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException(
                "That file could not be opened as a spreadsheet — if it is an old .xls, save it as .xlsx first.", ex);
        }
    }

    /// <summary>PDF → text, page by page in reading order (PdfPig). Password-protected or
    /// corrupted files, and scans with no text layer, throw the honest sentence instead of
    /// returning half a document.</summary>
    private static (string, string) ExtractPdf(byte[] content)
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
            var text = new StringBuilder();
            var pages = 0;
            var anyText = false;
            var truncated = false;

            foreach (var page in document.GetPages())
            {
                pages++;
                if (text.Length >= MaxChars) { truncated = true; break; }

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
                if (string.IsNullOrWhiteSpace(pageText)) continue;

                anyText = true;
                if (text.Length > 0) text.AppendLine();
                text.AppendLine($"[Page {page.Number}]");
                text.AppendLine(pageText.Trim());
            }

            if (!anyText)
            {
                throw new InvalidDataException(
                    "That PDF has no selectable text — it is likely a scan. Reading scans needs "
                    + "OCR, which is not available here; the figures have to come from the user.");
            }
            if (truncated)
                text.AppendLine($"[… the document was longer than {MaxChars:N0} characters and has been cut here.]");

            return (text.ToString().TrimEnd(),
                $"{pages} page{(pages == 1 ? "" : "s")}" + (truncated ? " (truncated)" : ""));
        }
    }

    private static readonly XNamespace WordNs =
        "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    /// <summary>Word (.docx) → text, read straight out of the document zip — no Office
    /// dependency. Paragraphs one per line, tabs and breaks honoured, tables as tab-separated
    /// rows, content controls unwrapped.</summary>
    private static (string, string) ExtractDocx(byte[] content)
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

            var text = new StringBuilder();
            var paragraphs = 0;
            var truncated = false;
            AppendDocxBlocks(body, text, ref paragraphs, ref truncated);

            var result = text.ToString().TrimEnd();
            if (string.IsNullOrWhiteSpace(result))
                throw new InvalidDataException("the document has no readable text");
            if (truncated)
                result += $"\n[… the document was longer than {MaxChars:N0} characters and has been cut here.]";

            return (result,
                $"{paragraphs:N0} paragraph{(paragraphs == 1 ? "" : "s")}" + (truncated ? " (truncated)" : ""));
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

    private static void AppendDocxBlocks(XElement container, StringBuilder text, ref int paragraphs, ref bool truncated)
    {
        foreach (var element in container.Elements())
        {
            if (text.Length >= MaxChars) { truncated = true; return; }

            if (element.Name == WordNs + "p")
            {
                paragraphs++;
                text.AppendLine(DocxParagraphText(element));
            }
            else if (element.Name == WordNs + "tbl")
            {
                foreach (var row in element.Elements(WordNs + "tr"))
                {
                    if (text.Length >= MaxChars) { truncated = true; return; }
                    text.AppendLine(string.Join('\t', row.Elements(WordNs + "tc").Select(DocxCellText)));
                }
            }
            else if (element.Name == WordNs + "sdt")
            {
                // A content control wraps real content — unwrap it.
                if (element.Element(WordNs + "sdtContent") is { } inner)
                    AppendDocxBlocks(inner, text, ref paragraphs, ref truncated);
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

    private static (string, string) ExtractText(byte[] content)
    {
        string text;
        try
        {
            // BOM-aware: Excel exports CSVs as UTF-16 (and UTF-8 with a BOM) often enough that
            // blind UTF-8 turned them to spaced-out mojibake. No BOM falls back to UTF-8.
            using var stream = new MemoryStream(content);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            text = reader.ReadToEnd().Trim();
        }
        catch (Exception ex)
        {
            throw new InvalidDataException("That file could not be read as text.", ex);
        }

        var truncated = text.Length > MaxChars;
        if (truncated)
            text = text[..MaxChars] + $"\n[… the file was longer than {MaxChars:N0} characters and has been cut here.]";

        var lines = text.Count(character => character == '\n') + 1;
        return (text, $"{lines:N0} line{(lines == 1 ? "" : "s")}" + (truncated ? " (truncated)" : ""));
    }
}
