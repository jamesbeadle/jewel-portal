using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

/// <summary>
/// The Contractor's Report as a PDF in the house style — the nine sections in PLG's order under
/// the clean header. Built from the composed document and the loaded photographs, nothing else,
/// so the same document gives the same pages (bar the provenance line).
/// </summary>
public static partial class ContractorsReportPdfRenderer
{
    public static byte[] Render(ContractorsReportDocument model, IReadOnlyDictionary<string, ContractorsReportImage> images, DateTimeOffset generatedAt)
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
        HouseFooter(section, ContractorsReportText.Provenance(generatedAt));

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
            new HeaderFact($"Period  {ContractorsReportText.Period(header)}"),
            new HeaderFact($"Date of issue  {ContractorsReportText.Date(header.DateOfIssue)}"));

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        var labelWidth = Unit.FromCentimeter(3.3);
        var valueWidth = Unit.FromCentimeter(5.6);
        table.AddColumn(labelWidth);
        table.AddColumn(valueWidth);
        table.AddColumn(labelWidth);
        table.AddColumn(valueWidth);
        AddGridRow(table, "Valuation No.", ContractorsReportText.ValuationNumber(header), "Programme reference", ContractorsReportText.OrDash(header.ProgrammeReference));
        AddGridRow(table, "Prepared by", ContractorsReportText.OrDash(header.PreparedByName), "Issued to", ContractorsReportText.OrDash(header.IssuedTo));
        SpaceAfterTable(section);
    }

    private static void AddGridRow(Table table, string firstLabel, string firstValue, string secondLabel, string secondValue)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        LabelCell(row.Cells[0], firstLabel);
        ValueCell(row.Cells[1], firstValue);
        LabelCell(row.Cells[2], secondLabel);
        ValueCell(row.Cells[3], secondValue);
    }

    private static void AddNarrative(Section section, string heading, string text)
    {
        SectionHeading(section, heading);
        Panelled(section, ContractorsReportText.OrNothingToReport(text));
        SpaceAfterTable(section);
    }

    private static void MutedLine(Section section, string text)
    {
        var line = section.AddParagraph(text);
        line.Format.Font.Size = 9;
        line.Format.Font.Italic = true;
        line.Format.Font.Color = Muted;
        SpaceAfter(line, 2);
    }
}
