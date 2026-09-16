using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Contracts.Progress;

using static Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents.ContractorsReportWordParts;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

/// <summary>
/// The Contractor's Report as a Word document — the same nine sections, in the same order and
/// words as the PDF, set in Word's own styles so the person can edit before it goes out. When
/// PLG's own template arrives, this renderer is the one place to swap the styles.
/// </summary>
public static partial class ContractorsReportWordRenderer
{
    private const int PageWidthTwips = 11906;
    private const int PageHeightTwips = 16838;
    private const int SideMarginTwips = 907;
    private const int TopMarginTwips = 1600;
    private const int BottomMarginTwips = 1134;
    private const int HeaderFooterDistanceTwips = 567;
    private const long LogoWidthEmu = 1_512_000;
    private const uint HeaderPictureId = 1000;

    public static byte[] Render(ContractorsReportDocument model, IReadOnlyDictionary<string, ContractorsReportImage> images, DateTimeOffset generatedAt)
    {
        using var stream = new MemoryStream();
        using (var word = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = word.AddMainDocumentPart();
            var body = new Body();
            mainPart.Document = new Document(body);

            AddTitle(body, model.Header);
            AddProgress(body, model);
            AddLookAhead(body, model);
            AddDecisions(body, model);
            AddVariations(body, model);
            AddNarrative(body, ContractorsReportSections.Neighbours, model.Neighbours);
            AddNarrative(body, ContractorsReportSections.HealthAndSafety, model.HealthAndSafety);
            AddBuildingControl(body, model.BuildingControl);
            AddSubcontractors(body, model);
            AddPhotographs(body, new ContractorsReportWordPictures(mainPart), model, images);
            body.Append(PageSetup(LogoHeader(mainPart), ProvenanceFooter(mainPart, generatedAt)));
            mainPart.Document.Save();
        }
        return stream.ToArray();
    }

    private static void AddTitle(Body body, ContractorsReportHeader header)
    {
        body.Append(Text(header.DocumentTitle, TitleSize, false, false, Navy, 20));
        body.Append(Text(header.ProjectName, BodySize, true, false, Gold, 120));
        body.Append(ContractorsReportWordTables.Grid(
            ("Project reference", header.ProjectReference), ("Period", ContractorsReportText.Period(header)),
            ("Valuation No.", ContractorsReportText.ValuationNumber(header)), ("Programme reference", header.ProgrammeReference),
            ("Prepared by", header.PreparedByName), ("Issued to", header.IssuedTo),
            ("Date of issue", ContractorsReportText.Date(header.DateOfIssue)), ("", "")));
        body.Append(new Paragraph());
    }

    private static void AddNarrative(Body body, string heading, string text)
    {
        body.Append(SectionHeading(heading));
        body.Append(Panelled(ContractorsReportText.OrNothingToReport(text)));
    }

    private static Paragraph MutedLine(string text) => Text(text, BodySize, false, true, Muted);

    private static string LogoHeader(MainDocumentPart mainPart)
    {
        var headerPart = mainPart.AddNewPart<HeaderPart>();
        var logoBytes = DocumentBranding.LogoPngBytes();
        var size = ImagePixelSize.Of(logoBytes) ?? (1, 1);
        var logo = new ContractorsReportImage("logo", logoBytes, "image/png", size.Width, size.Height);
        var picture = new ContractorsReportWordPictures(headerPart, HeaderPictureId).Picture(logo, LogoWidthEmu);
        picture.ParagraphProperties = new ParagraphProperties { Justification = new Justification { Val = JustificationValues.Center } };
        headerPart.Header = new Header(picture);
        return mainPart.GetIdOfPart(headerPart);
    }

    private static string ProvenanceFooter(MainDocumentPart mainPart, DateTimeOffset generatedAt)
    {
        var footerPart = mainPart.AddNewPart<FooterPart>();
        var line = new Paragraph(new ParagraphProperties
        {
            Tabs = new Tabs(new TabStop { Val = TabStopValues.Right, Position = PageWidthTwips - 2 * SideMarginTwips })
        });
        line.Append(Run("JEWEL BESPOKE BUILD", "14", true, false, Gold));
        line.Append(Run("   www.jewelbb.co.uk", "14", false, false, Muted));
        line.Append(new Run(new TabChar()));
        line.Append(Run(ContractorsReportText.Provenance(generatedAt), "14", false, false, Muted));
        footerPart.Footer = new Footer(line);
        return mainPart.GetIdOfPart(footerPart);
    }

    private static SectionProperties PageSetup(string headerId, string footerId) => new(
        new HeaderReference { Type = HeaderFooterValues.Default, Id = headerId },
        new FooterReference { Type = HeaderFooterValues.Default, Id = footerId },
        new PageSize { Width = PageWidthTwips, Height = PageHeightTwips },
        new PageMargin
        {
            Top = TopMarginTwips, Right = SideMarginTwips, Bottom = BottomMarginTwips, Left = SideMarginTwips,
            Header = HeaderFooterDistanceTwips, Footer = HeaderFooterDistanceTwips, Gutter = 0
        });
}
