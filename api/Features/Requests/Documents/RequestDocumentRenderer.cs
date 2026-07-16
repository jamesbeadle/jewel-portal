using System.Globalization;
using Jewel.JPMS.Models;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace Jewel.JPMS.Api.Features.Requests.Documents;

/// <summary>
/// Renders a <see cref="RequestDocumentModel"/> into a branded, Procore-style one-page request sheet
/// (PDF bytes) using PDFsharp/MigraDoc. Pure function of the model: no I/O, no database — the same
/// bytes come out for the same input (bar the generated-at footer), which keeps regeneration on
/// download/resend idempotent. Shared by the api (download) and worker (send) projects.
/// </summary>
public static class RequestDocumentRenderer
{
    // JewelBB palette. Orange is the primary identifier (eyebrow + hairlines + diamonds), Navy is the
    // atmosphere (header band + headings), Gold is the shared-enterprise luxury accent (URL + ref),
    // White carries headlines on Navy.
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

    public static byte[] Render(RequestDocumentModel model)
    {
        EnsureFonts();

        var document = new Document();
        document.Info.Title = $"{model.DisplayNumber} {model.TypeShort}".Trim();
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
        setup.BottomMargin = Unit.FromCentimeter(1.3);
        setup.LeftMargin = Unit.FromCentimeter(1.6);
        setup.RightMargin = Unit.FromCentimeter(1.6);

        AddHeaderBand(section, model);
        AddTitleBlock(section, model);
        AddPartiesGrid(section, model);
        AddReferences(section, model);
        AddQuestionSection(section, model);
        AddItemisedQueries(section, model);
        AddResponseActionRequired(section, model);
        AddResponseSection(section, model);
        AddRecipients(section, model);
        AddFooter(section, model);

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    // ---- Sections -----------------------------------------------------------------------------

    private static void AddHeaderBand(Section section, RequestDocumentModel model)
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

        // Left: orange eyebrow, white type name, gold reference line.
        var eyebrow = row.Cells[0].AddParagraph("JEWEL BESPOKE BUILD");
        eyebrow.Format.Font.Size = 7.5;
        eyebrow.Format.Font.Bold = true;
        eyebrow.Format.Font.Color = Orange;
        SpaceAfter(eyebrow, 1.5);

        var heading = row.Cells[0].AddParagraph(model.TypeLong.ToUpperInvariant());
        heading.Format.Font.Size = 17;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = White;
        SpaceAfter(heading, 1);

        var refLine = string.IsNullOrEmpty(model.DisplayReference)
            ? model.TypeShort
            : $"{model.DisplayReference}  ·  {model.TypeShort}";
        var sub = row.Cells[0].AddParagraph(refLine);
        sub.Format.Font.Size = 9.5;
        sub.Format.Font.Bold = true;
        sub.Format.Font.Color = Gold;

        // Right: status + key dates.
        var status = row.Cells[1].AddParagraph(model.StatusLabel.ToUpperInvariant());
        status.Format.Font.Size = 10;
        status.Format.Font.Bold = true;
        status.Format.Font.Color = model.IsOverdue ? Orange : White;
        SpaceAfter(status, 2);

        // The issued date is what the correspondent cares about; the recorded issue date when one has
        // been set on the request, falling back to the raised date until then (see IssuedDisplayDate).
        var issued = row.Cells[1].AddParagraph($"Issued  {Date(model.IssuedDisplayDate)}");
        issued.Format.Font.Size = 8;
        issued.Format.Font.Color = White;
        SpaceAfter(issued, 0.5);

        var dueText = model.ResponseDue is { } due ? Date(due) : "—";
        var due2 = row.Cells[1].AddParagraph($"Response due  {dueText}");
        due2.Format.Font.Size = 8;
        due2.Format.Font.Color = model.IsOverdue ? Orange : Gold;

        // Orange hairline directly beneath the band.
        Hairline(section);
    }

    private static void AddTitleBlock(Section section, RequestDocumentModel model)
    {
        var label = section.AddParagraph("SUBJECT");
        label.Format.Font.Size = 7.5;
        label.Format.Font.Bold = true;
        label.Format.Font.Color = Muted;
        SpaceBefore(label, 3);
        SpaceAfter(label, 1);

        var title = section.AddParagraph(model.Title);
        title.Format.Font.Size = 13;
        title.Format.Font.Bold = true;
        title.Format.Font.Color = Navy;
        SpaceAfter(title, 2);
    }

    private static void AddPartiesGrid(Section section, RequestDocumentModel model)
    {
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
            "Status", model.StatusLabel);
        AddGridRow(table,
            "Requesting party", model.RaisedByEmail,
            "Issued to", string.IsNullOrWhiteSpace(model.RaisedTo) ? "—" : model.RaisedTo!);

        SpaceAfterTable(section);
    }

    private static void AddReferences(Section section, RequestDocumentModel model)
    {
        var hasAny = !string.IsNullOrWhiteSpace(model.DrawingRef)
                     || !string.IsNullOrWhiteSpace(model.RelatedDrawingSpec);
        if (!hasAny)
            return;

        SectionHeading(section, "References");
        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(3.3));
        table.AddColumn(Unit.FromCentimeter(14.5));

        if (!string.IsNullOrWhiteSpace(model.DrawingRef))
            AddWideRow(table, "Drawing / detail", model.DrawingRef!);
        if (!string.IsNullOrWhiteSpace(model.RelatedDrawingSpec))
            AddWideRow(table, "Related drawing / spec", model.RelatedDrawingSpec!);

        SpaceAfterTable(section);
    }

    private static void AddQuestionSection(Section section, RequestDocumentModel model)
    {
        SectionHeading(section, "Question / Request");
        Panelled(section, string.IsNullOrWhiteSpace(model.Description) ? "—" : model.Description);

        // Commercial / programme impact line, when the request carries one.
        var impacts = new List<string>();
        if (model.ImpliesVariation)
            impacts.Add("Implies a variation");
        if (model.Value is { } v)
            impacts.Add($"Indicative value {v.ToString("C0", Uk)}");
        if (impacts.Count > 0)
        {
            var impact = section.AddParagraph();
            impact.AddFormattedText("Commercial / programme impact:  ", TextFormat.Bold);
            impact.AddText(string.Join("   ·   ", impacts));
            impact.Format.Font.Size = 8.5;
            impact.Format.Font.Color = Muted;
            SpaceBefore(impact, 2);
        }

        if (!string.IsNullOrWhiteSpace(model.ClientNotes))
        {
            var notes = section.AddParagraph();
            notes.AddFormattedText("Notes:  ", TextFormat.Bold);
            notes.AddText(model.ClientNotes!);
            notes.Format.Font.Size = 8.5;
            notes.Format.Font.Color = Muted;
            SpaceBefore(notes, 1.5);
        }

        SpaceAfterTable(section);
    }

    private static void AddItemisedQueries(Section section, RequestDocumentModel model)
    {
        // The basis-of-queries note leads the table when present, even if there are no items yet.
        var items = model.ItemList;
        if (items.Count == 0 && string.IsNullOrWhiteSpace(model.BasisOfQueries))
            return;

        SectionHeading(section, "Itemised queries");

        if (items.Count > 0)
        {
            var table = section.AddTable();
            table.Borders.Color = Hair;
            table.Borders.Width = 0.5;
            table.AddColumn(Unit.FromCentimeter(0.9));   // Item
            table.AddColumn(Unit.FromCentimeter(3.2));   // Drawing ref
            table.AddColumn(Unit.FromCentimeter(3.2));   // Member / area
            table.AddColumn(Unit.FromCentimeter(6.4));   // Query
            table.AddColumn(Unit.FromCentimeter(4.1));   // Response

            var head = table.AddRow();
            head.Shading.Color = Navy;
            head.HeadingFormat = true;                   // repeat the header when the table breaks pages
            HeaderCell(head.Cells[0], "Item");
            HeaderCell(head.Cells[1], "Drawing ref");
            HeaderCell(head.Cells[2], "Member / area");
            HeaderCell(head.Cells[3], "Query");
            HeaderCell(head.Cells[4], "Response");

            var zebra = false;
            foreach (var item in items)
            {
                var row = table.AddRow();
                if (zebra) row.Shading.Color = Panel;
                zebra = !zebra;
                BodyCell(row.Cells[0], item.Position.ToString());
                BodyCell(row.Cells[1], item.DrawingRef);
                BodyCell(row.Cells[2], item.MemberArea);
                BodyCell(row.Cells[3], item.Query);
                BodyCell(row.Cells[4], string.IsNullOrWhiteSpace(item.Response) ? "—" : item.Response!);
            }
        }

        if (!string.IsNullOrWhiteSpace(model.BasisOfQueries))
        {
            var basis = section.AddParagraph();
            basis.AddFormattedText("Basis of queries:  ", TextFormat.Bold);
            basis.AddText(model.BasisOfQueries!);
            basis.Format.Font.Size = 8.5;
            basis.Format.Font.Color = Muted;
            SpaceBefore(basis, 2);
        }

        SpaceAfterTable(section);
    }

    private static void AddResponseActionRequired(Section section, RequestDocumentModel model)
    {
        var hasAny = !string.IsNullOrWhiteSpace(model.ResponseActionRequired)
                     || !string.IsNullOrWhiteSpace(model.ImpactIfLate);
        if (!hasAny)
            return;

        SectionHeading(section, "Response / action required");

        if (!string.IsNullOrWhiteSpace(model.ResponseActionRequired))
            Panelled(section, model.ResponseActionRequired!);

        var footnotes = new List<(string Label, string Text)>();
        if (model.ResponseDue is { } due)
            footnotes.Add(("Required by:  ", Date(due)));
        if (!string.IsNullOrWhiteSpace(model.ImpactIfLate))
            footnotes.Add(("Impact if not received by the required date:  ", model.ImpactIfLate!));

        foreach (var (label, text) in footnotes)
        {
            var line = section.AddParagraph();
            line.AddFormattedText(label, TextFormat.Bold);
            line.AddText(text);
            line.Format.Font.Size = 8.5;
            line.Format.Font.Color = Muted;
            SpaceBefore(line, 1.5);
        }

        SpaceAfterTable(section);
    }

    private static void AddResponseSection(Section section, RequestDocumentModel model)
    {
        SectionHeading(section, "Response");

        if (string.IsNullOrWhiteSpace(model.ResponseText))
        {
            var awaiting = Panelled(section, "Awaiting response.");
            awaiting.Format.Font.Italic = true;
            awaiting.Format.Font.Color = Muted;
        }
        else
        {
            Panelled(section, model.ResponseText!);

            var by = new List<string>();
            if (!string.IsNullOrWhiteSpace(model.RespondedByEmail))
                by.Add(model.RespondedByEmail!);
            if (model.RespondedAt is { } at)
                by.Add(Date(at));
            if (by.Count > 0)
            {
                var line = section.AddParagraph("Responded by " + string.Join(" on ", by));
                line.Format.Font.Size = 8;
                line.Format.Font.Color = Muted;
                SpaceBefore(line, 1.5);
            }
        }

        SpaceAfterTable(section);
    }

    // The document lists who it is addressed to and who is openly copied, in two blocks. Bcc never
    // reaches the model (see RequestDocumentRecipient), so it cannot render here.
    private static void AddRecipients(Section section, RequestDocumentModel model)
    {
        SectionHeading(section, "Issued to");

        var to = model.Recipients.Where(r => r.Routing != CorrespondenceRouting.Cc).ToList();
        var copied = model.Recipients.Where(r => r.Routing == CorrespondenceRouting.Cc).ToList();

        if (to.Count == 0)
        {
            var none = section.AddParagraph(
                "No correspondent is set for this project. Link the project (or this request) to a " +
                "client or architect, or set a project contact's routing to To, so this request can be issued.");
            none.Format.Font.Size = 8.5;
            none.Format.Font.Italic = true;
            none.Format.Font.Color = Orange;
            SpaceAfterTable(section);
            return;
        }

        RecipientTable(section, to);

        if (copied.Count > 0)
        {
            SectionHeading(section, "Copied to");
            RecipientTable(section, copied);
        }
    }

    private static void RecipientTable(Section section, IReadOnlyList<RequestDocumentRecipient> recipients)
    {
        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(5.0));
        table.AddColumn(Unit.FromCentimeter(6.8));
        table.AddColumn(Unit.FromCentimeter(3.2));
        table.AddColumn(Unit.FromCentimeter(2.8));

        var head = table.AddRow();
        head.Shading.Color = Navy;
        HeaderCell(head.Cells[0], "Name");
        HeaderCell(head.Cells[1], "Email");
        HeaderCell(head.Cells[2], "Organisation");
        HeaderCell(head.Cells[3], "Role");

        var zebra = false;
        foreach (var r in recipients)
        {
            var row = table.AddRow();
            if (zebra) row.Shading.Color = Panel;
            zebra = !zebra;
            BodyCell(row.Cells[0], string.IsNullOrWhiteSpace(r.Name) ? r.Email : r.Name);
            BodyCell(row.Cells[1], r.Email);
            BodyCell(row.Cells[2], string.IsNullOrWhiteSpace(r.Organisation) ? "—" : r.Organisation!);
            BodyCell(row.Cells[3], r.Role);
        }

        SpaceAfterTable(section);
    }

    private static void AddFooter(Section section, RequestDocumentModel model)
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

        // Right-align the generated-at via a right tab stop at the usable width.
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

    private static void AddWideRow(Table table, string label, string value)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        LabelCell(row.Cells[0], label);
        ValueCell(row.Cells[1], value);
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

    private static void HeaderCell(Cell cell, string text)
    {
        cell.Format.LeftIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(text);
        // MigraDoc cell padding lives on the Row; emulate vertical padding via paragraph spacing.
        p.Format.SpaceBefore = Unit.FromMillimeter(1);
        p.Format.SpaceAfter = Unit.FromMillimeter(1);
        p.Format.Font.Size = 8;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = White;
    }

    private static void BodyCell(Cell cell, string text)
    {
        cell.Format.LeftIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(string.IsNullOrWhiteSpace(text) ? "—" : text);
        p.Format.SpaceBefore = Unit.FromMillimeter(0.8);
        p.Format.SpaceAfter = Unit.FromMillimeter(0.8);
        p.Format.Font.Size = 8.5;
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
