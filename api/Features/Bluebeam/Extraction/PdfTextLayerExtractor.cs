using System.Text.Json;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>
/// Reads the PDF's own embedded text layer and page geometry with PdfPig — no OCR, so a scanned
/// drawing with no text layer legitimately comes back with empty pages, and that emptiness is
/// itself information. Reading order beats content-stream order for the same reason it does in
/// AiSourceReader: title blocks and notes read stream-wise interleave into nonsense.
/// </summary>
public static class PdfTextLayerExtractor
{
    public sealed record PageGeometry(int Page, double WidthPoints, double HeightPoints, int Rotation);
    public sealed record TextLayer(IReadOnlyList<DrawingTextPage> Pages, IReadOnlyList<PageGeometry> Geometry);

    public static TextLayer Read(byte[] pdfBytes)
    {
        UglyToad.PdfPig.PdfDocument document;
        try
        {
            document = UglyToad.PdfPig.PdfDocument.Open(pdfBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "The PDF could not be opened for text extraction — it may be password-protected or corrupted.", ex);
        }

        using (document)
        {
            var pages = new List<DrawingTextPage>();
            var geometry = new List<PageGeometry>();
            foreach (var page in document.GetPages())
            {
                pages.Add(new DrawingTextPage(page.Number, ReadPageText(page)));
                geometry.Add(new PageGeometry(
                    page.Number, page.Width, page.Height, (int)page.Rotation.Value));
            }
            return new TextLayer(pages, geometry);
        }
    }

    public static string GeometryJson(IReadOnlyList<PageGeometry> geometry) =>
        JsonSerializer.Serialize(geometry);

    private static string ReadPageText(UglyToad.PdfPig.Content.Page page)
    {
        try
        {
            return UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor
                .ContentOrderTextExtractor.GetText(page);
        }
        catch (Exception)
        {
            return page.Text;
        }
    }
}
