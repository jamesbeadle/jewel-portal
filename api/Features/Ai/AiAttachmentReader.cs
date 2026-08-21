using System.Text;
using ClosedXML.Excel;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// Turns an uploaded chat attachment into text the model can read. Extraction happens ONCE, at
/// upload — the text is what gets persisted (as a Context row), the bytes are discarded. Kept
/// deliberately narrow: spreadsheets and plain text, the formats the boss's tracker actually comes
/// in — plus images (pasted screenshots, site photos), which are the one kind persisted as BYTES
/// because the model reads them as image blocks, not text. PDFs and Word documents are refused
/// with a plain sentence rather than half-read — the same honesty rule as read_email_attachment
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

    private static readonly string[] SupportedExtensions = { ".xlsx", ".csv", ".tsv", ".txt" };

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

    public static string SupportedList => "xlsx, csv, tsv, txt — or an image (png, jpg, gif, webp)";

    /// <summary>
    /// Checks an image upload actually IS the image its name claims (magic bytes, not extension —
    /// a renamed .exe must not ride into the transcript as a "png") and returns the one-line human
    /// summary for the pill. Throws <see cref="InvalidDataException"/> when the bytes don't match.
    /// </summary>
    public static string ValidateImage(string fileName, byte[] content)
    {
        var mediaType = ImageMediaType(fileName);
        var looksRight = mediaType switch
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
        if (!looksRight)
            throw new InvalidDataException(
                $"\"{fileName}\" doesn't look like a real {Path.GetExtension(fileName).TrimStart('.')} image — re-save it and attach again.");

        return content.Length >= 1_048_576
            ? $"image · {content.Length / 1_048_576.0:0.#} MB"
            : $"image · {Math.Max(1, content.Length / 1024)} KB";
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

    private static (string, string) ExtractText(byte[] content)
    {
        string text;
        try
        {
            text = Encoding.UTF8.GetString(content).TrimStart('﻿').Trim();
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
