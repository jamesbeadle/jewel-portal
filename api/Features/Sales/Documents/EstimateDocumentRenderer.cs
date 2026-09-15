using Jewel.JPMS.Api.Features.Requests.Documents;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Sales.Documents;

/// <summary>
/// The estimate sheet (2026-09-15, Nigel: "I want to see it — a PDF export from the page"): one
/// <see cref="LeadEstimate"/> with its lead, rendered in the house style — the navy band, the
/// details grid, the scope, and the notes laid out line by line so a breakdown typed into them
/// (a heading, "- item: £x" lines, a total) reads as a breakdown. It is the INTERNAL record: the
/// footer says so, and the client-facing document stays the proposal. Pure function of the model
/// bar the generated-at stamp; regenerated on every download, nothing stored. The priced
/// breakdown will replace the notes-as-breakdown once the estimating workbook is in.
/// </summary>
public static class EstimateDocumentRenderer
{
    public sealed record Model(LeadEstimate Estimate, Lead Lead, DateTimeOffset GeneratedAt);

    public static byte[] Render(Model model)
    {
        EnsureFonts();

        var estimate = model.Estimate;
        var lead = model.Lead;

        var document = new Document();
        document.Info.Title = $"{estimate.Reference} — Estimate — {PropertyLine(lead)}";
        document.Info.Author = "Jewel Bespoke Build";
        document.Info.Subject = estimate.Scope;

        var normal = document.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;

        var section = A4Page(document);
        AddHeaderBand(section, model);
        AddDetailsGrid(section, model);

        SectionHeading(section, "Scope");
        Panelled(section, string.IsNullOrWhiteSpace(estimate.Scope) ? "—" : estimate.Scope);
        SpaceAfterTable(section);

        AddFigures(section, estimate);
        AddNotes(section, estimate.Notes);

        HouseFooter(section,
            $"Generated {DateAndTime(model.GeneratedAt)} · {estimate.Reference} on {lead.Reference} · internal estimate, not for issue — the proposal is the client's document");

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    /// <summary>"EST-0001 - 16 Ravens Dene - estimate.pdf": the reference, the property (else the
    /// contact), and what it is — the download name the browser shows.</summary>
    public static string FileName(LeadEstimate estimate, Lead lead)
    {
        var who = string.IsNullOrWhiteSpace(lead.PropertyAddress)
            ? (string.IsNullOrWhiteSpace(lead.ContactName) ? lead.CompanyName : lead.ContactName)
            : lead.PropertyAddress.Split(',')[0].Trim();
        var name = string.IsNullOrWhiteSpace(who) ? $"{estimate.Reference} - estimate.pdf" : $"{estimate.Reference} - {who} - estimate.pdf";
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }

    // ---- Sections -----------------------------------------------------------------------------

    private static void AddHeaderBand(Section section, Model model)
    {
        var table = section.AddTable();
        table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(11.3));
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

        DocumentBranding.AddLogo(row.Cells[0], Unit.FromCentimeter(3.4), Unit.FromMillimeter(1.5));

        var heading = row.Cells[0].AddParagraph("ESTIMATE");
        heading.Format.Font.Size = 17;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = White;
        SpaceAfter(heading, 1);

        var sub = row.Cells[0].AddParagraph(PropertyLine(model.Lead));
        sub.Format.Font.Size = 9.5;
        sub.Format.Font.Bold = true;
        sub.Format.Font.Color = Gold;

        var reference = row.Cells[1].AddParagraph(model.Estimate.Reference);
        reference.Format.Font.Size = 10;
        reference.Format.Font.Bold = true;
        reference.Format.Font.Color = White;
        SpaceAfter(reference, 2);

        var status = row.Cells[1].AddParagraph($"Status  {model.Estimate.Status.DisplayName()}");
        status.Format.Font.Size = 8;
        status.Format.Font.Color = Gold;

        Hairline(section);
    }

    private static void AddDetailsGrid(Section section, Model model)
    {
        var estimate = model.Estimate;
        var lead = model.Lead;

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

        var contact = string.IsNullOrWhiteSpace(lead.ContactName) ? lead.CompanyName : lead.ContactName;
        if (!string.IsNullOrWhiteSpace(lead.CompanyName) && !string.IsNullOrWhiteSpace(lead.ContactName))
            contact = $"{lead.ContactName}, {lead.CompanyName}";

        AddGridRow(table, "Lead", $"{lead.Reference} — {contact}", "Estimate", estimate.Reference);
        AddGridRow(table, "Property", PropertyLine(lead), "Status", $"{estimate.Status.DisplayName()} since {Date(estimate.StatusChangedAt)}");
        AddGridRow(table, "Architect", estimate.ArchitectName, "Price due", estimate.PriceDueOn is { } due ? due.ToString("dd MMM yyyy", Uk) : "—");
        AddGridRow(table, "Contact", string.Join("  ", new[] { lead.ContactEmail, lead.ContactPhone }.Where(value => !string.IsNullOrWhiteSpace(value))),
            "Submitted", estimate.SubmittedAt is { } submitted ? Date(submitted) : "—");
        AddGridRow(table, "Opened by", estimate.CreatedByEmail, "Opened", Date(estimate.CreatedAt));

        SpaceAfterTable(section);
    }

    /// <summary>The money, on its own: budget mentioned beside the total — the two figures anyone
    /// picking the sheet up wants first.</summary>
    private static void AddFigures(Section section, LeadEstimate estimate)
    {
        SectionHeading(section, "Figures");

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(8.9));
        var right = table.AddColumn(Unit.FromCentimeter(8.9));

        var header = table.AddRow();
        header.Shading.Color = Panel;
        LabelCell(header.Cells[0], "Budget the prospect mentioned");
        LabelCell(header.Cells[1], "Estimated total (excluding VAT)");

        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.5);
        row.BottomPadding = Unit.FromMillimeter(1.5);
        var budget = row.Cells[0].AddParagraph(estimate.BudgetMentioned is { } mentioned ? WholeMoney(mentioned) : "—");
        budget.Format.LeftIndent = CellIndent;
        budget.Format.Font.Size = 12;
        budget.Format.Font.Color = Ink;
        var total = row.Cells[1].AddParagraph(estimate.Total is { } priced ? WholeMoney(priced) : "Not yet priced");
        total.Format.LeftIndent = CellIndent;
        total.Format.Font.Size = 14;
        total.Format.Font.Bold = true;
        total.Format.Font.Color = estimate.Total is null ? Muted : Navy;

        SpaceAfterTable(section);
    }

    /// <summary>The notes, line by line. A line ending in " — £x" or in a colon-and-figure reads as
    /// a heading with its subtotal; a "- " line is an item, indented, with its figure pushed to a
    /// right tab; a line starting TOTAL is bold; anything else is a paragraph. Blank lines are
    /// the gaps between blocks. Nothing is parsed into numbers — the text prints as typed.</summary>
    private static void AddNotes(Section section, string notes)
    {
        var lines = (notes ?? "").Replace("\r\n", "\n").Split('\n').Select(line => line.TrimEnd()).ToList();
        if (lines.All(string.IsNullOrWhiteSpace)) return;

        SectionHeading(section, "Notes and breakdown");

        var previousBlank = false;
        foreach (var raw in lines)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                previousBlank = true;
                continue;
            }

            var line = raw.Trim();
            var isItem = line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("• ", StringComparison.Ordinal);
            var isTotal = line.StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase);
            var isHeading = !isItem && !isTotal && line.Length <= 60 && line.Contains(" — £", StringComparison.Ordinal);
            var isShout = !isItem && !isTotal && !isHeading && line.Length <= 40 && line == line.ToUpperInvariant() && line.Any(char.IsLetter);

            Paragraph paragraph;
            if (isItem)
            {
                var body = line[2..].Trim();
                var (label, figure) = SplitFigure(body);
                paragraph = section.AddParagraph();
                paragraph.Format.LeftIndent = Unit.FromMillimeter(4);
                paragraph.Format.Font.Size = 8.8;
                paragraph.Format.TabStops.AddTabStop(Unit.FromCentimeter(17.8), TabAlignment.Right);
                paragraph.AddText(label);
                if (figure is not null)
                {
                    paragraph.AddTab();
                    paragraph.AddFormattedText(figure, new Font { Bold = false });
                }
                paragraph.Format.Borders.Bottom.Width = 0.25;
                paragraph.Format.Borders.Bottom.Color = Hair;
                paragraph.Format.Borders.Distance = Unit.FromMillimeter(0.8);
                SpaceBefore(paragraph, 0.8);
                SpaceAfter(paragraph, 0.8);
            }
            else if (isTotal)
            {
                var (label, figure) = SplitFigure(line);
                paragraph = section.AddParagraph();
                paragraph.Format.Font.Size = 10;
                paragraph.Format.Font.Bold = true;
                paragraph.Format.Font.Color = Navy;
                paragraph.Format.TabStops.AddTabStop(Unit.FromCentimeter(17.8), TabAlignment.Right);
                paragraph.AddText(label);
                if (figure is not null) { paragraph.AddTab(); paragraph.AddText(figure); }
                paragraph.Format.Borders.Top.Width = 0.75;
                paragraph.Format.Borders.Top.Color = Orange;
                paragraph.Format.Borders.Distance = Unit.FromMillimeter(1.2);
                SpaceBefore(paragraph, 2);
                SpaceAfter(paragraph, 2);
            }
            else if (isHeading)
            {
                var at = line.LastIndexOf(" — £", StringComparison.Ordinal);
                paragraph = section.AddParagraph();
                paragraph.Format.Font.Size = 9.5;
                paragraph.Format.Font.Bold = true;
                paragraph.Format.Font.Color = Navy;
                paragraph.Format.KeepWithNext = true;
                paragraph.Format.TabStops.AddTabStop(Unit.FromCentimeter(17.8), TabAlignment.Right);
                paragraph.AddText(line[..at]);
                paragraph.AddTab();
                paragraph.AddText(line[(at + 3)..]);
                paragraph.Format.Shading.Color = Panel;
                SpaceBefore(paragraph, previousBlank ? 3 : 1.5);
                SpaceAfter(paragraph, 1);
            }
            else if (isShout)
            {
                paragraph = section.AddParagraph(line);
                paragraph.Format.Font.Size = 8;
                paragraph.Format.Font.Bold = true;
                paragraph.Format.Font.Color = Muted;
                paragraph.Format.KeepWithNext = true;
                SpaceBefore(paragraph, previousBlank ? 3 : 1.5);
                SpaceAfter(paragraph, 0.8);
            }
            else
            {
                paragraph = section.AddParagraph(line);
                paragraph.Format.Font.Size = 9;
                SpaceBefore(paragraph, previousBlank ? 2 : 0);
                SpaceAfter(paragraph, 0.8);
            }
            previousBlank = false;
        }
    }

    // ---- Helpers ------------------------------------------------------------------------------

    /// <summary>"Label: £9,500" → ("Label", "£9,500"); a line without a trailing figure keeps its
    /// text whole. The figure is whatever follows the last ": £" — text, not a parsed number.</summary>
    private static (string Label, string? Figure) SplitFigure(string text)
    {
        var at = text.LastIndexOf(": £", StringComparison.Ordinal);
        if (at < 0) at = text.LastIndexOf(" £", StringComparison.Ordinal) is var space && space > 0 && text[(space + 1)..].Skip(1).All(c => char.IsDigit(c) || c == ',' || c == '.') ? space : -1;
        if (at < 0) return (text, null);
        var label = text[..at].TrimEnd(':', ' ');
        var figure = text[at..].TrimStart(':', ' ');
        return (label, figure);
    }

    private static void AddGridRow(Table table, string label1, string value1, string label2, string value2)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1);
        row.BottomPadding = Unit.FromMillimeter(1);
        LabelCell(row.Cells[0], label1);
        ValueCell(row.Cells[1], value1);
        LabelCell(row.Cells[2], label2);
        ValueCell(row.Cells[3], value2);
    }

    private static string PropertyLine(Lead lead)
    {
        var parts = new[] { lead.PropertyAddress, lead.Postcode }.Where(value => !string.IsNullOrWhiteSpace(value));
        var line = string.Join(", ", parts);
        if (!string.IsNullOrWhiteSpace(line)) return line;
        return string.IsNullOrWhiteSpace(lead.ContactName) ? lead.CompanyName : lead.ContactName;
    }

    private static string WholeMoney(decimal value) => value.ToString("C0", Uk);
}
