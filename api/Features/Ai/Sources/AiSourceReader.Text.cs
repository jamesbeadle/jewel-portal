using System.Text;
using Ganss.Xss;
using Jewel.JPMS.Api.Features.Requests;

namespace Jewel.JPMS.Api.Features.Ai.Sources;

internal static partial class AiSourceReader
{
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
}
