using System.Globalization;
using Jewel.JPMS.Api.Features.Requests.Documents;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace Jewel.JPMS.Api.Features.Progress.Documents;

/// <summary>
/// Renders a <see cref="ProgressReportDocumentModel"/> into a branded, client-facing progress
/// report (PDF bytes) using PDFsharp/MigraDoc. Pure function of the model: the same bytes come out
/// for the same input (bar the generated-at footer), so regeneration on download is idempotent.
/// Follows the JewelBB palette established by <see cref="RequestDocumentRenderer"/>.
/// </summary>
public static class ProgressReportRenderer
{
    // JewelBB palette. Orange is the primary identifier (eyebrow + hairlines), Navy is the
    // atmosphere (header band + headings), Gold is the shared-enterprise luxury accent.
    private static readonly Color Navy = new(0x1A, 0x1E, 0x29);
    private static readonly Color Orange = new(0xFF, 0x83, 0x00);
    private static readonly Color Gold = new(0xC0, 0x9A, 0x51);
    private static readonly Color White = new(0xFF, 0xFF, 0xFF);
    private static readonly Color Panel = new(0xF3, 0xF3, 0xF5);
    private static readonly Color Hair = new(0xDD, 0xDD, 0xE1);
    private static readonly Color Muted = new(0x60, 0x66, 0x72);
    private static readonly Color Ink = new(0x22, 0x26, 0x30);

    private const string FontFamily = "JPMS Sans";
    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");

    private static readonly object FontGate = new();
    private static bool _fontsReady;

    public static byte[] Render(ProgressReportDocumentModel model)
    {
        EnsureFonts();

        var document = new Document();
        document.Info.Title = $"{model.ProjectReference} Progress Report".Trim();
        document.Info.Author = "Jewel Bespoke Build";
        document.Info.Subject = model.Title;

        var normal = document.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;

        var section = document.AddSection();
        var setup = section.PageSetup;
        setup.PageFormat = PageFormat.A4;
        setup.TopMargin = Unit.FromCentimeter(1.3);
        setup.BottomMargin = Unit.FromCentimeter(1.6);
        setup.LeftMargin = Unit.FromCentimeter(1.6);
        setup.RightMargin = Unit.FromCentimeter(1.6);

        AddHeaderBand(section, model);
        AddDetailsGrid(section, model);
        AddNarrative(section, "Introduction", model.Introduction);
        AddNarrative(section, "Work completed", model.WorkCompleted);
        AddUpdates(section, model);
        AddNarrative(section, "Upcoming works", model.UpcomingWorks);
        AddFooter(section, model);

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    // ---- Sections -----------------------------------------------------------------------------

    private static void AddHeaderBand(Section section, ProgressReportDocumentModel model)
    {
        var table = section.AddTable();
        table.Borders.Width = 0;
        var left = table.AddColumn(Unit.FromCentimeter(11.3));
        var right = table.AddColumn(Unit.FromCentimeter(6.5));
        right.Format.Alignment = ParagraphAlignment.Right;

        var row = table.AddRow();
        row.Shading.Color = Navy;
        row.TopPadding = Unit.FromMillimeter(4);
        row.BottomPadding = Unit.FromMillimeter(4);
        row.Cells[0].Format.LeftIndent = Unit.FromMillimeter(4);
        row.Cells[1].Format.RightIndent = Unit.FromMillimeter(4);
        row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
        row.Cells[1].VerticalAlignment = VerticalAlignment.Center;

        var eyebrow = row.Cells[0].AddParagraph("JEWEL BESPOKE BUILD");
        eyebrow.Format.Font.Size = 7.5;
        eyebrow.Format.Font.Bold = true;
        eyebrow.Format.Font.Color = Orange;
        SpaceAfter(eyebrow, 1.5);

        var heading = row.Cells[0].AddParagraph("PROGRESS REPORT");
        heading.Format.Font.Size = 17;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = White;
        SpaceAfter(heading, 1);

        var sub = row.Cells[0].AddParagraph(model.Title);
        sub.Format.Font.Size = 9.5;
        sub.Format.Font.Bold = true;
        sub.Format.Font.Color = Gold;

        var project = row.Cells[1].AddParagraph(model.ProjectReference.ToUpperInvariant());
        project.Format.Font.Size = 10;
        project.Format.Font.Bold = true;
        project.Format.Font.Color = White;
        SpaceAfter(project, 2);

        var period = row.Cells[1].AddParagraph($"Period  {Period(model.PeriodStart, model.PeriodEnd)}");
        period.Format.Font.Size = 8;
        period.Format.Font.Color = Gold;

        Hairline(section);
    }

    private static void AddDetailsGrid(Section section, ProgressReportDocumentModel model)
    {
        var spacer = section.AddParagraph();
        spacer.Format.SpaceAfter = Unit.FromMillimeter(1.5);
        spacer.Format.Font.Size = 2;

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        var labelW = Unit.FromCentimeter(3.3);
        var valueW = Unit.FromCentimeter(5.6);
        table.AddColumn(labelW);
        table.AddColumn(valueW);
        table.AddColumn(labelW);
        table.AddColumn(valueW);

        AddGridRow(table,
            "Project", model.ProjectName,
            "Project reference", model.ProjectReference);
        AddGridRow(table,
            "Client", string.IsNullOrWhiteSpace(model.ClientName) ? "—" : model.ClientName,
            "Report period", Period(model.PeriodStart, model.PeriodEnd));
        AddGridRow(table,
            "Prepared by", model.PreparedByEmail,
            "Report date", Date(model.GeneratedAt));

        SpaceAfterTable(section);
    }

    private static void AddNarrative(Section section, string heading, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        SectionHeading(section, heading);
        Panelled(section, text);
        SpaceAfterTable(section);
    }

    private static void AddUpdates(Section section, ProgressReportDocumentModel model)
    {
        if (model.Updates.Count == 0)
            return;

        SectionHeading(section, "Progress of the works");

        foreach (var update in model.Updates)
        {
            var title = section.AddParagraph(update.Title);
            title.Format.Font.Size = 10;
            title.Format.Font.Bold = true;
            title.Format.Font.Color = Navy;
            title.Format.KeepWithNext = true;
            SpaceBefore(title, 4);
            SpaceAfter(title, 1);

            if (update.WorkDate is { } workDate)
            {
                var when = section.AddParagraph(Date(workDate));
                when.Format.Font.Size = 8;
                when.Format.Font.Color = Gold;
                when.Format.Font.Bold = true;
                when.Format.KeepWithNext = true;
                SpaceAfter(when, 1.5);
            }

            if (!string.IsNullOrWhiteSpace(update.Description))
            {
                var description = section.AddParagraph(update.Description);
                description.Format.Font.Size = 9.5;
                SpaceAfter(description, 2);
            }

            AddPhotoGrid(section, update.Photos);
        }

        SpaceAfterTable(section);
    }

    // Photos sit two-up in a borderless table; MigraDoc keeps the aspect ratio, so rows grow to
    // the taller of their two images.
    private static void AddPhotoGrid(Section section, IReadOnlyList<ProgressReportDocumentPhoto> photos)
    {
        if (photos.Count == 0)
            return;

        var table = section.AddTable();
        table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(8.9));
        table.AddColumn(Unit.FromCentimeter(8.9));

        for (var index = 0; index < photos.Count; index += 2)
        {
            var row = table.AddRow();
            row.TopPadding = Unit.FromMillimeter(1);
            row.BottomPadding = Unit.FromMillimeter(1);
            AddPhotoCell(row.Cells[0], photos[index]);
            if (index + 1 < photos.Count)
                AddPhotoCell(row.Cells[1], photos[index + 1]);
        }

        SpaceAfterTable(section);
    }

    private static void AddPhotoCell(Cell cell, ProgressReportDocumentPhoto photo)
    {
        // MigraDoc accepts image data inline via the base64: pseudo-filename, which keeps the
        // renderer a pure function of the model (no temp files).
        var image = cell.AddImage("base64:" + Convert.ToBase64String(photo.Content));
        image.LockAspectRatio = true;
        image.Width = Unit.FromCentimeter(8.6);
    }

    private static void AddFooter(Section section, ProgressReportDocumentModel model)
    {
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Borders.Top.Width = 0.75;
        footer.Format.Borders.Top.Color = Orange;
        footer.Format.Borders.Distance = Unit.FromMillimeter(2);
        footer.Format.Font.Size = 7.5;

        footer.AddFormattedText("◆ ", new Font { Color = Orange, Size = 7.5 });
        footer.AddFormattedText("JEWEL BESPOKE BUILD", new Font { Color = Navy, Bold = true, Size = 7.5 });
        footer.AddFormattedText("    WWW.JEWELBB.CO.UK", new Font { Color = Gold, Bold = true, Size = 7.5 });
        footer.AddTab();
        footer.AddFormattedText(
            $"Generated {DateTime(model.GeneratedAt)} · from the JPMS register (source of truth)",
            new Font { Color = Muted, Size = 7 });

        footer.Format.TabStops.AddTabStop(Unit.FromCentimeter(18.3), TabAlignment.Right);
    }

    // ---- Helpers ------------------------------------------------------------------------------

    private static void SectionHeading(Section section, string text)
    {
        var p = section.AddParagraph(text);
        p.Format.Font.Size = 10.5;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Navy;
        p.Format.Borders.Bottom.Width = 0.75;
        p.Format.Borders.Bottom.Color = Orange;
        p.Format.Borders.Distance = Unit.FromMillimeter(1.5);
        p.Format.KeepWithNext = true;
        SpaceBefore(p, 4);
        SpaceAfter(p, 2.5);
    }

    private static Paragraph Panelled(Section section, string text)
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
        var p = row.Cells[0].AddParagraph(text);
        p.Format.Font.Size = 9.5;
        return p;
    }

    private static void AddGridRow(Table table, string l1, string v1, string l2, string v2)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        LabelCell(row.Cells[0], l1);
        ValueCell(row.Cells[1], v1);
        LabelCell(row.Cells[2], l2);
        ValueCell(row.Cells[3], v2);
    }

    private static void LabelCell(Cell cell, string text)
    {
        cell.Shading.Color = Panel;
        cell.Format.LeftIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(text);
        p.Format.Font.Size = 8;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Muted;
    }

    private static void ValueCell(Cell cell, string text)
    {
        cell.Format.LeftIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(string.IsNullOrWhiteSpace(text) ? "—" : text);
        p.Format.Font.Size = 9;
        p.Format.Font.Color = Ink;
    }

    private static void Hairline(Section section)
    {
        var table = section.AddTable();
        table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(17.8));
        var row = table.AddRow();
        row.Height = Unit.FromMillimeter(0.9);
        row.HeightRule = RowHeightRule.Exactly;
        row.Cells[0].Shading.Color = Orange;
    }

    private static void SpaceBefore(Paragraph p, double mm) => p.Format.SpaceBefore = Unit.FromMillimeter(mm);
    private static void SpaceAfter(Paragraph p, double mm) => p.Format.SpaceAfter = Unit.FromMillimeter(mm);

    private static void SpaceAfterTable(Section section)
    {
        var spacer = section.AddParagraph();
        spacer.Format.SpaceAfter = Unit.FromMillimeter(2);
        spacer.Format.Font.Size = 2;
    }

    private static string Period(DateTimeOffset? start, DateTimeOffset? end) => (start, end) switch
    {
        ({ } s, { } e) => $"{Date(s)} – {Date(e)}",
        ({ } s, null) => $"From {Date(s)}",
        (null, { } e) => $"To {Date(e)}",
        _ => "—"
    };

    private static string Date(DateTimeOffset value) => value.ToString("dd MMM yyyy", Uk);
    private static string DateTime(DateTimeOffset value) => value.ToString("dd MMM yyyy HH:mm", Uk);

    private static void EnsureFonts()
    {
        if (_fontsReady)
            return;
        lock (FontGate)
        {
            if (_fontsReady)
                return;
            // FontResolver is a global, set-once setting; only install ours if nothing else has.
            GlobalFontSettings.FontResolver ??= new DocumentFontResolver();
            _fontsReady = true;
        }
    }
}
