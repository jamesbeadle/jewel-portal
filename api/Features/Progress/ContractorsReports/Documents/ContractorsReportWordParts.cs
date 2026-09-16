using DocumentFormat.OpenXml.Wordprocessing;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

/// <summary>
/// The Word building blocks the Contractor's Report is set in — the house palette and type as
/// Open XML runs and paragraphs. Word cannot carry the PDF's font, so the body is Calibri in the
/// same colours, sizes and rules. Properties are set through the SDK's typed members, which
/// keep the schema order Word insists on.
/// </summary>
internal static class ContractorsReportWordParts
{
    public const string Navy = "1A1E29";
    public const string Orange = "FF8300";
    public const string Gold = "C09A51";
    public const string Muted = "606672";
    public const string Ink = "222630";
    public const string PanelFill = "F3F3F5";
    public const string HairFill = "DDDDE1";
    public const string White = "FFFFFF";
    public const string BodyFont = "Calibri";

    // Word measures type in half-points and layout in twentieths of a point.
    public const string BodySize = "19";
    public const string SmallSize = "16";
    public const string DaySize = "18";
    public const string HeadingSize = "21";
    public const string TitleSize = "34";
    public const int TwipsPerCentimetre = 567;

    public static Paragraph Text(string text, string size = BodySize, bool isBold = false, bool isItalic = false, string colour = Ink, int spaceAfterTwips = 80) =>
        new(Properties(spaceAfterTwips), Run(text, size, isBold, isItalic, colour));

    public static Paragraph Lines(string text, string size = BodySize, string colour = Ink)
    {
        var paragraph = new Paragraph(Properties(80));
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            if (index > 0) paragraph.Append(new Run(new Break()));
            paragraph.Append(Run(lines[index], size, false, false, colour));
        }
        return paragraph;
    }

    public static Run Run(string text, string size, bool isBold, bool isItalic, string colour)
    {
        var properties = new RunProperties
        {
            RunFonts = new RunFonts { Ascii = BodyFont, HighAnsi = BodyFont },
            Bold = isBold ? new Bold() : null,
            Italic = isItalic ? new Italic() : null,
            Color = new Color { Val = colour },
            FontSize = new FontSize { Val = size }
        };
        return new Run(properties, new Text(text) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve });
    }

    public static Paragraph SectionHeading(string text)
    {
        var properties = Properties(100);
        properties.KeepNext = new KeepNext();
        properties.ParagraphBorders = new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Color = Orange, Size = 6, Space = 2 });
        properties.SpacingBetweenLines = new SpacingBetweenLines { Before = "200", After = "100" };
        return new Paragraph(properties, Run(text, HeadingSize, true, false, Navy));
    }

    public static Paragraph DayHeading(string text)
    {
        var properties = Properties(60);
        properties.KeepNext = new KeepNext();
        properties.SpacingBetweenLines = new SpacingBetweenLines { Before = "120", After = "60" };
        return new Paragraph(properties, Run(text, DaySize, true, false, Gold));
    }

    public static Paragraph Bullet(string text)
    {
        var properties = Properties(40);
        properties.Indentation = new Indentation { Left = "360", Hanging = "360" };
        return new Paragraph(properties, Run("•\t" + text, BodySize, false, false, Ink));
    }

    public static Paragraph Panelled(string text)
    {
        var paragraph = Lines(text);
        paragraph.ParagraphProperties!.Shading = new Shading { Val = ShadingPatternValues.Clear, Fill = PanelFill };
        paragraph.ParagraphProperties!.Indentation = new Indentation { Left = "140", Right = "140" };
        return paragraph;
    }

    public static Paragraph Aligned(Paragraph paragraph, JustificationValues justification)
    {
        paragraph.ParagraphProperties ??= new ParagraphProperties();
        paragraph.ParagraphProperties.Justification = new Justification { Val = justification };
        return paragraph;
    }

    private static ParagraphProperties Properties(int spaceAfterTwips) => new()
    {
        SpacingBetweenLines = new SpacingBetweenLines { After = spaceAfterTwips.ToString(), Line = "252", LineRule = LineSpacingRuleValues.Auto }
    };
}
