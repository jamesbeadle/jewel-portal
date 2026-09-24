using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;

using static Jewel.JPMS.Api.Features.Documents.DocumentTables;
using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

/// <summary>
/// The Contractor's Report as a PDF in the house style — the nine sections in PLG's order under
/// the clean header. Built from the composed document and the loaded photographs, nothing else,
/// so the same document gives the same pages. It goes to the client, so its footer is the
/// company, the report and the page — no provenance note.
/// </summary>
public static partial class ContractorsReportPdfRenderer
{
    public static byte[] Render(ContractorsReportDocument model, IReadOnlyDictionary<string, ContractorsReportImage> images)
    {
        EnsureFonts();

        var document = new Document();
        document.Info.Title = $"{model.Header.ProjectReference} {model.Header.DocumentTitle}";
        document.Info.Author = "Jewel Bespoke Build";
        document.Info.Subject = model.Header.ProjectName;

        var normal = document.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;

        var section = A4Page(document);
        AddHeader(section, model.Header);
        AddProgress(section, model);
        AddLookAhead(section, model);
        AddDecisions(section, model);
        AddVariations(section, model);
        AddNarrative(section, ContractorsReportSections.Neighbours, model.Neighbours);
        AddNarrative(section, ContractorsReportSections.HealthAndSafety, model.HealthAndSafety);
        AddBuildingControl(section, model.BuildingControl);
        AddSubcontractors(section, model);
        AddPhotographs(section, model, images);
        HouseFooterWithPageNumbers(section, ContractorsReportPrintedText.FooterLead(model.Header));

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    private static void AddHeader(Section section, ContractorsReportHeader header)
    {
        CleanHeader(section, header.DocumentTitle, header.ProjectName,
            new HeaderFact(header.ProjectReference.ToUpperInvariant()),
            new HeaderFact($"Date of issue  {ContractorsReportPrintedText.DayAndDate(header.DateOfIssue)}"));

        var table = GridTable(section);
        GridRow(table, "Project", ContractorsReportPrintedText.ProjectLine(header), "Reporting period", ContractorsReportPrintedText.PeriodLong(header));
        GridRow(table, "Valuation No.", ContractorsReportText.ValuationNumber(header), "Programme reference", ContractorsReportText.OrDash(header.ProgrammeReference));
        GridRow(table, "Prepared by", ContractorsReportText.OrDash(header.PreparedByName), "Issued to", ContractorsReportText.OrDash(header.IssuedTo));
        SpaceAfterTable(section);
    }

    private static void AddNarrative(Section section, string heading, string text)
    {
        SectionHeading(section, heading);
        Panelled(section, ContractorsReportText.OrNothingToReport(text));
        SpaceAfterTable(section);
    }
}
