using System.IO.Compression;
using System.Xml.Linq;

namespace Jewel.JPMS.Api.Features.Ai.Sources;

internal static partial class AiSourceReader
{
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
}
