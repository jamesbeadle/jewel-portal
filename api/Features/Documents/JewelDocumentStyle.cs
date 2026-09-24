using System.Globalization;
using Jewel.JPMS.Api.Features.Requests.Documents;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using PdfSharp.Fonts;

namespace Jewel.JPMS.Api.Features.Documents;

/// <summary>
/// The house style every generated Jewel document shares — the JewelBB palette, the one font
/// family, and the table/paragraph helpers renderers lean on — so official documents read as one
/// family. Fonts come from the same DocumentFontResolver.
/// </summary>
internal static class JewelDocumentStyle
{
    // JewelBB palette — Orange identifies, Navy sets the atmosphere, Gold is the luxury accent.
    public static readonly Color Navy = new(0x1A, 0x1E, 0x29);
    public static readonly Color Orange = new(0xFF, 0x83, 0x00);
    public static readonly Color Gold = new(0xC0, 0x9A, 0x51);
    public static readonly Color White = new(0xFF, 0xFF, 0xFF);
    public static readonly Color Panel = new(0xF3, 0xF3, 0xF5);
    public static readonly Color Hair = new(0xDD, 0xDD, 0xE1);
    public static readonly Color Muted = new(0x60, 0x66, 0x72);
    public static readonly Color Ink = new(0x22, 0x26, 0x30);

    public const string FontFamily = "JPMS Sans";
    public static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");
    /// <summary>The text inset every table cell shares, so columns line up across documents.</summary>
    public static readonly Unit CellIndent = Unit.FromMillimeter(1.5);

    private static readonly object FontGate = new();
    private static bool _fontsReady;

    public static void EnsureFonts()
    {
        if (_fontsReady) return;
        lock (FontGate)
        {
            if (_fontsReady) return;
            // FontResolver is a global, set-once setting; only install ours if nothing else has.
            GlobalFontSettings.FontResolver ??= new DocumentFontResolver();
            _fontsReady = true;
        }
    }

    /// <summary>
    /// THE page geometry every house document shares: A4, 1.6 cm sides, 1.3 cm top — and a bottom
    /// margin that CLEARS the footer. MigraDoc hangs the footer FooterDistance up from the page
    /// edge and grows it upward from there by its content height, while the body runs down to
    /// BottomMargin; if the two overlap the footer prints straight through the last table row on
    /// any full page (the 2026-09-07 bug, and again on 2026-09-16 — see <see cref="OrangeBand"/>
    /// for the second cause). Every renderer was carrying its own copy of these numbers (and the
    /// valuation snapshot had fixed the overlap locally) — this is the one place the numbers live
    /// now, and <see cref="FooterHeight"/> is the one place the footer's height is stated.
    /// </summary>
    public static Section A4Page(Document document)
    {
        var section = document.AddSection();
        var setup = section.PageSetup;
        setup.PageFormat = PageFormat.A4;
        setup.TopMargin = Unit.FromCentimeter(1.3);
        // The footer hangs from the very edge of the page (distance 0) so its orange band bleeds
        // off the bottom, and it is FooterHeight tall; the body stops FooterClearance above it.
        setup.FooterDistance = Unit.FromCentimeter(0);
        setup.BottomMargin = FooterHeight + FooterClearance;
        setup.LeftMargin = Unit.FromCentimeter(1.6);
        setup.RightMargin = Unit.FromCentimeter(1.6);
        return section;
    }

    // Declaration order matters below: C# runs static initialisers top to bottom, so the band's
    // numbers must be assigned before FooterHeight adds them up.

    /// <summary>The orange brand band along the foot of every page.</summary>
    public static readonly Unit BandHeight = Unit.FromMillimeter(9);

    /// <summary>The gap between the provenance line and the band.</summary>
    public static readonly Unit BandGap = Unit.FromMillimeter(3);

    /// <summary>
    /// The house footer's height as MigraDoc lays it out: the 7 pt provenance line (≈ 3 mm with
    /// its line spacing), the gap above the band, and the band itself. Stated here, next to the
    /// margins that must clear it, rather than rediscovered from a print-out.
    /// </summary>
    public static readonly Unit FooterHeight = Unit.FromMillimeter(3) + BandGap + BandHeight;

    /// <summary>White space between the last line of the body and the top of the footer.</summary>
    public static readonly Unit FooterClearance = Unit.FromMillimeter(6);

    /// <summary>
    /// THE house footer: brand and site on the left, the document's provenance note right-aligned
    /// ("Generated 07 Sep 2026 14:02 · from the JPMS register (source of truth)"), and the orange
    /// band beneath. One footer for every document, so the sheets read as one family — and one
    /// height (<see cref="FooterHeight"/>), which is what <see cref="A4Page"/>'s bottom margin is
    /// sized to clear.
    /// </summary>
    public static void HouseFooter(Section section, string note)
    {
        var footer = FooterLine(section);
        footer.AddFormattedText(note, FooterNoteFont());
        OrangeBand(section);
    }

    /// <summary>The house footer for a document that goes to the client: "Jewel Bespoke Build Ltd ·
    /// Contractor's Report No. 31 · Page 2 of 17" on the right instead of a provenance note.</summary>
    public static void HouseFooterWithPageNumbers(Section section, string lead)
    {
        var footer = FooterLine(section);
        footer.AddFormattedText($"{lead} · Page ", FooterNoteFont());
        footer.AddPageField();
        footer.AddFormattedText(" of ", FooterNoteFont());
        footer.AddNumPagesField();
        OrangeBand(section);
    }

    // The footer line sits just above the band, quietly: brand on the left, the note on the right
    // (a right tab stop at the usable width, 21 cm − 2 × 1.6 cm).
    private static Paragraph FooterLine(Section section)
    {
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Font.Size = 7;
        footer.Format.Font.Color = Muted;
        footer.Format.SpaceAfter = BandGap;   // the gap belongs to the line, not the band (see OrangeBand)
        footer.AddFormattedText("JEWEL BESPOKE BUILD", new Font { Color = Gold, Bold = true, Size = 7 });
        footer.AddFormattedText("   www.jewelbb.co.uk", new Font { Color = Muted, Size = 7 });
        footer.AddTab();
        footer.Format.TabStops.AddTabStop(Unit.FromCentimeter(17.8), TabAlignment.Right);
        return footer;
    }

    private static Font FooterNoteFont() => new() { Color = Muted, Size = 7 };

    /// <summary>
    /// The brand band along the foot of every page (2026-09-15, Nigel: the documents follow the
    /// tender's branding — a clean white top with the logo, an orange band across the bottom). A
    /// text frame that sits IN THE FOOTER'S FLOW, straight under the provenance line, positioned
    /// against the page only horizontally so it bleeds to both side edges; with the footer hung
    /// from the page edge (FooterDistance 0, see <see cref="A4Page"/>) the band's bottom IS the
    /// bottom of the page. It lives in the footer so every page carries it.
    ///
    /// Why not simply pin it to the page bottom with RelativeVertical.Page (2026-09-16)? MigraDoc
    /// measures a footer from its first element's top to its LAST element's bottom, and a
    /// page-positioned frame reports its absolute page position as that bottom — so the footer's
    /// measured height became the whole bottom margin and it was hung FooterDistance higher than
    /// the body's end: the provenance line printed through the last table row on every full
    /// page (the VO export, 16 Sep). In the flow, the footer measures what it draws.
    ///
    /// The band carries no top distance of its own: MigraDoc counts a first element's top margin
    /// in the footer's height but draws the element at the footer's top regardless, so a
    /// band-only footer (the estimate's cover) with a distance would float that far off the
    /// page edge. The gap under the provenance line is that paragraph's SpaceAfter instead.
    /// </summary>
    public static void OrangeBand(Section section)
    {
        var band = section.Footers.Primary.AddTextFrame();
        band.RelativeHorizontal = RelativeHorizontal.Page;
        band.RelativeVertical = RelativeVertical.Paragraph;   // in the footer's flow, under the line
        band.WrapFormat.Style = WrapStyle.TopBottom;            // and taking up its own height there
        band.Left = Unit.FromCentimeter(0);
        band.Width = Unit.FromCentimeter(21);
        band.Height = BandHeight;
        band.FillFormat.Color = Orange;
        band.LineFormat.Width = 0;
    }

    /// <summary>
    /// THE clean document top (2026-09-15): the logo centred on white, then the title with its
    /// subtitle on the left and the document's key facts (a reference, a status, dates) on the
    /// right — no band, no box. Replaces the navy header band every renderer used to draw; the
    /// facts each document carried there are carried here, only the ground has gone.
    /// </summary>
    public static void CleanHeader(Section section, string title, string? subtitle, params HeaderFact[] facts)
    {
        // The logo is the page header, so every page of the document carries it, centred, as the
        // tender's pages do; the body starts 3.3 cm down to clear it.
        var logo = section.Headers.Primary.AddParagraph();
        logo.Format.Alignment = ParagraphAlignment.Center;
        var image = logo.AddImage(DocumentBranding.LogoImageName);
        image.Width = Unit.FromCentimeter(4.2);
        image.LockAspectRatio = true;
        section.PageSetup.HeaderDistance = Unit.FromCentimeter(1.0);
        section.PageSetup.TopMargin = Unit.FromCentimeter(3.3);

        var table = section.AddTable();
        table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(11.3));
        var right = table.AddColumn(Unit.FromCentimeter(6.5));
        right.Format.Alignment = ParagraphAlignment.Right;
        var row = table.AddRow();
        row.VerticalAlignment = VerticalAlignment.Bottom;

        var heading = row.Cells[0].AddParagraph(title);
        heading.Format.Font.Size = 17;
        heading.Format.Font.Color = Navy;
        SpaceAfter(heading, subtitle is null ? 0 : 1);
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            var sub = row.Cells[0].AddParagraph(subtitle);
            sub.Format.Font.Size = 9.5;
            sub.Format.Font.Bold = true;
            sub.Format.Font.Color = Gold;
        }

        var first = true;
        foreach (var fact in facts)
        {
            if (string.IsNullOrWhiteSpace(fact.Text)) continue;
            var line = row.Cells[1].AddParagraph(fact.Text);
            line.Format.Font.Size = first ? 10 : 8;
            line.Format.Font.Bold = first || fact.Bold;
            line.Format.Font.Color = fact.Color ?? (first ? Navy : Muted);
            SpaceAfter(line, first ? 2 : 0.5);
            first = false;
        }

        // A fine gold rule closes the top; the body starts beneath it.
        var rule = section.AddParagraph();
        rule.Format.Borders.Bottom.Width = 0.5;
        rule.Format.Borders.Bottom.Color = Gold;
        rule.Format.Font.Size = 2;
        rule.Format.SpaceBefore = Unit.FromMillimeter(2);
        rule.Format.SpaceAfter = Unit.FromMillimeter(3);
    }

    /// <summary>One right-hand line of the clean header. The first fact prints large; Color
    /// overrides the default (navy for the first, muted for the rest) — orange for "overdue".</summary>
    public sealed record HeaderFact(string Text, Color? Color = null, bool Bold = false);

    public static void SectionHeading(Section section, string text)
    {
        var paragraph = section.AddParagraph(text);
        paragraph.Format.Font.Size = 10.5;
        paragraph.Format.Font.Bold = true;
        paragraph.Format.Font.Color = Navy;
        paragraph.Format.Borders.Bottom.Width = 0.75;
        paragraph.Format.Borders.Bottom.Color = Orange;
        paragraph.Format.Borders.Distance = Unit.FromMillimeter(1.5);
        SpaceBefore(paragraph, 4);
        SpaceAfter(paragraph, 2.5);
    }

    public static Paragraph Panelled(Section section, string text)
    {
        var table = section.AddTable();
        table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(17.8));
        var row = table.AddRow();
        row.Shading.Color = Panel;
        row.TopPadding = Unit.FromMillimeter(2.5);
        row.BottomPadding = Unit.FromMillimeter(2.5);
        row.Cells[0].Format.LeftIndent = Unit.FromMillimeter(2.5);
        row.Cells[0].Format.RightIndent = Unit.FromMillimeter(2.5);
        var paragraph = row.Cells[0].AddParagraph(text);
        paragraph.Format.Font.Size = 9.5;
        return paragraph;
    }

    public static void LabelCell(Cell cell, string text)
    {
        cell.Shading.Color = Panel;
        cell.Format.LeftIndent = CellIndent;
        var paragraph = cell.AddParagraph(text);
        paragraph.Format.Font.Size = 8;
        paragraph.Format.Font.Bold = true;
        paragraph.Format.Font.Color = Muted;
    }

    public static void ValueCell(Cell cell, string text)
    {
        cell.Format.LeftIndent = CellIndent;
        var paragraph = cell.AddParagraph(string.IsNullOrWhiteSpace(text) ? "—" : text);
        paragraph.Format.Font.Size = 9;
        paragraph.Format.Font.Color = Ink;
    }

    public static void HeaderCell(Cell cell, string text)
    {
        cell.Format.LeftIndent = CellIndent;
        var paragraph = cell.AddParagraph(text);
        // MigraDoc cell padding lives on the Row; emulate vertical padding via paragraph spacing.
        paragraph.Format.SpaceBefore = Unit.FromMillimeter(1);
        paragraph.Format.SpaceAfter = Unit.FromMillimeter(1);
        paragraph.Format.Font.Size = 8;
        paragraph.Format.Font.Bold = true;
        paragraph.Format.Font.Color = White;
    }

    public static void BodyCell(Cell cell, string text)
    {
        cell.Format.LeftIndent = CellIndent;
        var paragraph = cell.AddParagraph(string.IsNullOrWhiteSpace(text) ? "—" : text);
        paragraph.Format.SpaceBefore = Unit.FromMillimeter(0.8);
        paragraph.Format.SpaceAfter = Unit.FromMillimeter(0.8);
        paragraph.Format.Font.Size = 8.5;
        paragraph.Format.Font.Color = Ink;
    }

    public static void Hairline(Section section)
    {
        var table = section.AddTable();
        table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(17.8));
        var row = table.AddRow();
        row.Height = Unit.FromMillimeter(0.9);
        row.HeightRule = RowHeightRule.Exactly;
        row.Cells[0].Shading.Color = Orange;
    }

    public static void SpaceBefore(Paragraph paragraph, double millimetres) =>
        paragraph.Format.SpaceBefore = Unit.FromMillimeter(millimetres);

    public static void SpaceAfter(Paragraph paragraph, double millimetres) =>
        paragraph.Format.SpaceAfter = Unit.FromMillimeter(millimetres);

    public static void SpaceAfterTable(Section section)
    {
        var spacer = section.AddParagraph();
        spacer.Format.SpaceAfter = Unit.FromMillimeter(2);
        spacer.Format.Font.Size = 2;
    }

    public static string Date(DateTimeOffset value) => value.ToString("dd MMM yyyy", Uk);
    public static string DateAndTime(DateTimeOffset value) => value.ToString("dd MMM yyyy HH:mm", Uk);
    public static string Money(decimal value) => value.ToString("C2", Uk);
}
