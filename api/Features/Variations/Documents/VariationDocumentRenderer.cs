using Jewel.JPMS.Api.Features.Requests.Documents;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Variations.Documents;

/// <summary>
/// Renders a <see cref="VariationDocumentModel"/> into the branded variation order sheet (PDF
/// bytes) using PDFsharp/MigraDoc — the same house style as the request (RFI) document. Pure
/// function of the model: no I/O, no database, so regeneration on download/attach/resend is
/// idempotent (two renders of an unchanged order differ only by the generated-at footer).
/// </summary>
public static class VariationDocumentRenderer
{
    public static byte[] Render(VariationDocumentModel model)
    {
        EnsureFonts();

        var document = new Document();
        document.Info.Title = $"{model.DocumentReference} {VariationDocumentModel.DocumentName}".Trim();
        document.Info.Author = "Jewel Bespoke Build";
        document.Info.Subject = model.Title;

        var normal = document.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;

        var section = A4Page(document);

        AddHeaderBand(section, model);
        VariationDocumentSections.AddTitleBlock(section, model);
        VariationDocumentSections.AddDetailsGrid(section, model);
        VariationDocumentSections.AddScopeOfWorks(section, model);
        VariationDocumentSections.AddNarrative(section, "Commercial basis", model.CommercialBasis);
        VariationDocumentCostBreakdown.Add(section, model);
        VariationDocumentSections.AddNarrative(section, "Programme impact", model.ProgrammeImpact);
        VariationDocumentSections.AddNarrative(section, "Exclusions", model.Exclusions);
        VariationDocumentSections.AddFooter(section, model);

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    private static void AddHeaderBand(Section section, VariationDocumentModel model)
    {
        // The clean house top (2026-09-15). The reference is the outgoing one alone ("VO32" — the
        // internal VOQ quoting reference never prints, Nigel 2026-09-14); on the right the stage
        // the variation has reached and the dates the correspondent cares about.
        var facts = new List<HeaderFact>
        {
            new(model.StatusLabel.ToUpperInvariant()),
            new($"Issued  {Date(model.IssuedDisplayDate)}")
        };
        if (model.ApprovedAt is { } approvedAt) facts.Add(new HeaderFact($"Approved  {Date(approvedAt)}", Gold));
        CleanHeader(section, VariationDocumentModel.DocumentName, model.DocumentReference, facts.ToArray());
    }
}
