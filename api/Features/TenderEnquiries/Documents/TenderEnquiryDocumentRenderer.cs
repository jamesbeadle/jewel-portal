using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Documents;

/// <summary>
/// Renders a <see cref="TenderEnquiryDocumentModel"/> into the branded PQQ response (PDF bytes)
/// using PDFsharp/MigraDoc — the same house style as the request and variation documents. Pure
/// function of the model: no I/O, no database, so regeneration on download and attach is
/// idempotent (two renders of unchanged answers differ only by the generated-at footer).
/// </summary>
public static class TenderEnquiryDocumentRenderer
{
    private const string Author = "Jewel Bespoke Build";

    public static byte[] Render(TenderEnquiryDocumentModel model)
    {
        EnsureFonts();

        var document = new Document();
        document.Info.Title = $"{model.Reference} PQQ Response";
        document.Info.Author = Author;
        document.Info.Subject = model.Title;

        var normal = document.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;

        var section = document.AddSection();
        var setup = section.PageSetup;
        setup.PageFormat = PageFormat.A4;
        setup.TopMargin = Unit.FromCentimeter(1.3);
        setup.BottomMargin = Unit.FromCentimeter(1.3);
        setup.LeftMargin = Unit.FromCentimeter(1.6);
        setup.RightMargin = Unit.FromCentimeter(1.6);

        TenderEnquiryDocumentChrome.AddHeaderBand(section, model);
        TenderEnquiryDocumentSections.AddTitleBlock(section, model);
        TenderEnquiryDocumentSections.AddDetailsGrid(section, model);
        TenderEnquiryDocumentSections.AddScopeOfWorks(section, model);
        TenderEnquiryDocumentSections.AddAnswers(section, model);
        TenderEnquiryDocumentChrome.AddFooter(section, model);

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }
}
