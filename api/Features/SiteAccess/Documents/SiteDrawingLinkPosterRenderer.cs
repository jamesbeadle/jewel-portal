using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.SiteAccess.Documents;

/// <summary>Renders the poster in the house style: the logo, the project and the label at the
/// top, the instruction and the QR code large in the middle, the dates in small print.</summary>
public static class SiteDrawingLinkPosterRenderer
{
    private const string Instruction = "Scan for the current drawings";
    private const string SmallPrint =
        "No sign-in and no app: the page opens in the phone's browser and shows the approved revision of each drawing, or the newest, marked as not approved.";
    private const double InstructionSize = 26;
    private const double LabelSize = 16;
    private const double BodySize = 11;
    private const double SmallPrintSize = 10;
    private const double ExpirySize = 9;
    private static readonly Unit QrWidth = Unit.FromCentimeter(7);

    public static byte[] Render(SiteDrawingLinkPoster poster)
    {
        EnsureFonts();
        var document = NewDocument(poster);
        var section = A4Page(document);
        CleanHeader(section, "Current drawings", poster.Label, new HeaderFact(poster.ProjectName), new HeaderFact(poster.ProjectReference));
        AddInstruction(section, poster);
        AddQrCode(section, poster);
        AddSmallPrint(section, poster);
        HouseFooter(section, $"Issued {Date(poster.IssuedAt)} · expires {Date(poster.ExpiresAt)}");
        return ToPdfBytes(document);
    }

    private static Document NewDocument(SiteDrawingLinkPoster poster)
    {
        var document = new Document();
        var info = document.Info;
        info.Title = $"Site drawings — {poster.Label}";
        info.Author = "Jewel Bespoke Build";
        var normal = document.Styles["Normal"]!;
        var font = normal.Font;
        font.Name = FontFamily;
        font.Size = BodySize;
        font.Color = Ink;
        return document;
    }

    private static void AddInstruction(Section section, SiteDrawingLinkPoster poster)
    {
        var instruction = Centred(section, Instruction, InstructionSize, Navy, isBold: true);
        SpaceBefore(instruction, 30);
        SpaceAfter(instruction, 4);
        Centred(section, poster.Label, LabelSize, Gold);
    }

    private static void AddQrCode(Section section, SiteDrawingLinkPoster poster)
    {
        var paragraph = Centred(section, "", BodySize, Ink);
        SpaceBefore(paragraph, 16);
        SpaceAfter(paragraph, 16);
        var image = paragraph.AddImage("base64:" + Convert.ToBase64String(poster.QrPng));
        image.Width = QrWidth;
        image.LockAspectRatio = true;
    }

    private static void AddSmallPrint(Section section, SiteDrawingLinkPoster poster)
    {
        var line = Centred(section, SmallPrint, SmallPrintSize, Muted);
        SpaceAfter(line, 3);
        Centred(section, $"This poster works until {Date(poster.ExpiresAt)} unless the office withdraws it sooner.", ExpirySize, Muted);
    }

    private static Paragraph Centred(Section section, string text, double size, Color colour, bool isBold = false)
    {
        var paragraph = section.AddParagraph(text);
        var format = paragraph.Format;
        format.Alignment = ParagraphAlignment.Center;
        var font = format.Font;
        font.Size = size;
        font.Color = colour;
        font.Bold = isBold;
        return paragraph;
    }

    private static byte[] ToPdfBytes(Document document)
    {
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }
}
