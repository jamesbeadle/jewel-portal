using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes.Charts;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Sales.Documents;

/// <summary>
/// The estimate document (2026-09-15, Nigel: "make sure we complete the estimate as expected" —
/// the shape of the Wodeland Avenue tender): a cover, the project page (what is estimated, the
/// property, the architect, the date, who to talk to at Jewel), the executive summary with the
/// build time and the exclusions, the breakdown chart, one itemised table per section (ID /
/// description / quantity / unit / unit price / total, a provisional allowance said so in red,
/// the section's total in the orange cell), the total project estimate value, and the contact
/// page. Client-facing: the internal notes never print. Pure function of the record bar the
/// generated-at stamp; regenerated on every download, nothing stored.
/// </summary>
public static class EstimateDocumentRenderer
{
    public sealed record Model(LeadEstimate Estimate, Lead Lead, DateTimeOffset GeneratedAt);

    private static readonly Color Provisional = new(0xD9, 0x2D, 0x3A);
    private const string DocumentTitle = "Estimate for project";

    public static byte[] Render(Model model)
    {
        EnsureFonts();

        var estimate = model.Estimate;
        var lead = model.Lead;

        var document = new Document();
        document.Info.Title = $"{estimate.Reference} — {DocumentTitle} — {PropertyLine(lead)}";
        document.Info.Author = "Jewel Bespoke Build";
        document.Info.Subject = estimate.Scope;

        var normal = document.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9.5;
        normal.Font.Color = Ink;

        AddCover(document, model);

        var section = A4Page(document);
        CleanHeader(section, DocumentTitle, PropertyLine(lead),
            new HeaderFact(estimate.Reference),
            new HeaderFact(Date(estimate.SubmittedAt ?? model.GeneratedAt)),
            new HeaderFact(estimate.Status.IsOpen() ? "" : estimate.Status.DisplayName().ToUpperInvariant()));
        AddProjectBlock(section, model);
        AddNarrative(section, model);
        AddChart(section, estimate);
        AddSections(section, estimate);
        AddGrandTotal(section, estimate);
        AddContactPage(section);
        HouseFooter(section, $"{estimate.Reference} · {PropertyLine(lead)} · {Date(model.GeneratedAt)}");

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

    // ---- Cover --------------------------------------------------------------------------------

    private static void AddCover(Document document, Model model)
    {
        var cover = A4Page(document);
        OrangeBand(cover);

        // The logo sits a little above centre with the title beneath it, as on the tender's cover.
        var space = cover.AddParagraph();
        space.Format.SpaceBefore = Unit.FromCentimeter(9.5);
        space.Format.Font.Size = 2;

        var logo = cover.AddParagraph();
        logo.Format.Alignment = ParagraphAlignment.Center;
        var image = logo.AddImage(Requests.Documents.DocumentBranding.LogoImageName);
        image.Width = Unit.FromCentimeter(7.6);
        image.LockAspectRatio = true;
        logo.Format.SpaceAfter = Unit.FromMillimeter(6);

        var title = cover.AddParagraph(DocumentTitle.ToUpperInvariant());
        title.Format.Alignment = ParagraphAlignment.Center;
        title.Format.Font.Size = 24;
        title.Format.Font.Color = Ink;
        SpaceAfter(title, 4);

        var property = cover.AddParagraph(PropertyLine(model.Lead));
        property.Format.Alignment = ParagraphAlignment.Center;
        property.Format.Font.Size = 11;
        property.Format.Font.Color = Muted;
        SpaceAfter(property, 1);

        var reference = cover.AddParagraph($"{model.Estimate.Reference}  ·  {Date(model.Estimate.SubmittedAt ?? model.GeneratedAt)}");
        reference.Format.Alignment = ParagraphAlignment.Center;
        reference.Format.Font.Size = 9;
        reference.Format.Font.Color = Gold;
        reference.Format.Font.Bold = true;
    }

    // ---- The project page -------------------------------------------------------------------

    private static void AddProjectBlock(Section section, Model model)
    {
        var estimate = model.Estimate;
        var lead = model.Lead;

        Labelled(section, "Estimate for:", estimate.Scope);
        Labelled(section, "Project:", PropertyLine(lead));
        Labelled(section, "Client:", ClientLine(lead));
        if (!string.IsNullOrWhiteSpace(estimate.ArchitectName)) Labelled(section, "Architect:", estimate.ArchitectName);
        Labelled(section, "Date:", Date(estimate.SubmittedAt ?? model.GeneratedAt));
        if (estimate.PriceDueOn is { } due) Labelled(section, "Price due:", due.ToString("dd MMMM yyyy", Uk));

        var gap = section.AddParagraph();
        gap.Format.SpaceAfter = Unit.FromMillimeter(3);
        gap.Format.Font.Size = 2;

        foreach (var line in CompanyBlock(estimate))
        {
            var paragraph = section.AddParagraph(line);
            paragraph.Format.Font.Size = 9.5;
            SpaceAfter(paragraph, 0.4);
        }
    }

    private static void Labelled(Section section, string label, string value)
    {
        var paragraph = section.AddParagraph();
        paragraph.Format.Font.Size = 11;
        paragraph.Format.LeftIndent = Unit.FromCentimeter(2.6);
        paragraph.Format.FirstLineIndent = Unit.FromCentimeter(-2.6);
        paragraph.Format.TabStops.AddTabStop(Unit.FromCentimeter(2.6), TabAlignment.Left);
        paragraph.AddFormattedText(label, new Font { Bold = true, Color = Ink });
        paragraph.AddTab();
        paragraph.AddText(string.IsNullOrWhiteSpace(value) ? "—" : value);
        SpaceAfter(paragraph, 2.5);
    }

    private static IEnumerable<string> CompanyBlock(LeadEstimate estimate)
    {
        yield return "Jewel Bespoke Build Ltd";
        yield return "Argent House";
        yield return "175 Hook Rise South, Surbiton";
        yield return "KT6 7LD";
        yield return "+44 (0)208 109 1014";
        if (!string.IsNullOrWhiteSpace(estimate.CreatedByEmail)) yield return estimate.CreatedByEmail;
        yield return "www.jewelbb.co.uk";
    }

    // ---- Executive summary, build time, exclusions --------------------------------------------

    private static void AddNarrative(Section section, Model model)
    {
        var estimate = model.Estimate;
        var hasSummary = !string.IsNullOrWhiteSpace(estimate.ExecutiveSummary);
        var hasBuildTime = !string.IsNullOrWhiteSpace(estimate.BuildTime);
        var hasExclusions = !string.IsNullOrWhiteSpace(estimate.Exclusions);
        if (!hasSummary && !hasBuildTime && !hasExclusions && estimate.Total is null) return;

        // Follows the project block on the same page — the tender's second page is the project
        // and its story together; the chart and the tables get pages of their own.
        var gap = section.AddParagraph();
        gap.Format.SpaceBefore = Unit.FromMillimeter(6);
        gap.Format.Font.Size = 2;
        SectionTitle(section, "Executive summary");

        if (hasSummary) Prose(section, estimate.ExecutiveSummary);

        if (estimate.Total is { } total)
        {
            var figure = section.AddParagraph();
            figure.Format.Font.Size = 10.5;
            SpaceBefore(figure, hasSummary ? 3 : 0);
            SpaceAfter(figure, 3);
            figure.AddFormattedText("Estimated total, excluding VAT:  ", new Font { Bold = true });
            figure.AddFormattedText(WholeMoney(total), new Font { Bold = true, Color = Navy, Size = 12 });
            if (estimate.BudgetMentioned is { } budget)
                figure.AddFormattedText($"    (budget mentioned {WholeMoney(budget)})", new Font { Color = Muted, Size = 9 });
        }

        if (hasBuildTime)
        {
            SubHeading(section, "Build time");
            Prose(section, estimate.BuildTime);
        }
        if (hasExclusions)
        {
            SubHeading(section, "Exclusions");
            Prose(section, estimate.Exclusions);
        }
    }

    /// <summary>Text as typed: blank lines separate paragraphs, a "- " line is a bullet.</summary>
    private static void Prose(Section section, string text)
    {
        foreach (var raw in (text ?? "").Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            var bullet = line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("• ", StringComparison.Ordinal);
            var paragraph = section.AddParagraph();
            paragraph.Format.Font.Size = 9.5;
            if (bullet)
            {
                paragraph.Format.LeftIndent = Unit.FromMillimeter(5);
                paragraph.Format.FirstLineIndent = Unit.FromMillimeter(-3.5);
                paragraph.Format.TabStops.AddTabStop(Unit.FromMillimeter(5), TabAlignment.Left);
                paragraph.AddText("•");
                paragraph.AddTab();
                paragraph.AddText(line[2..].Trim());
            }
            else
            {
                paragraph.AddText(line);
            }
            SpaceAfter(paragraph, bullet ? 0.8 : 2);
        }
    }

    // ---- The chart ----------------------------------------------------------------------------

    private static void AddChart(Section section, LeadEstimate estimate)
    {
        var sections = estimate.Sections.Where(s => s.Lines.Count > 0).ToList();
        if (sections.Count == 0) return;

        section.AddPageBreak();
        SectionTitle(section, "Project breakdown");
        var intro = section.AddParagraph("What each part of the works comes to, in pounds excluding VAT.");
        intro.Format.Font.Size = 9;
        intro.Format.Font.Color = Muted;
        SpaceAfter(intro, 4);

        // Horizontal bars so every section name reads whole — the tender's rotated labels need a
        // rotation MigraDoc's axes do not have.
        var chart = section.AddChart(ChartType.Bar2D);
        chart.Width = Unit.FromCentimeter(17.8);
        chart.Height = Unit.FromCentimeter(Math.Clamp(1.4 + sections.Count * 0.85, 6, 18));
        chart.Format.Font.Size = 8;
        chart.PlotArea.LineFormat.Width = 0;

        var series = chart.SeriesCollection.AddSeries();
        series.Name = "Project breakdown £";
        series.FillFormat.Color = Gold;
        series.LineFormat.Width = 0;
        var names = chart.XValues.AddXSeries();
        // Bar2D lists categories bottom-up; feed them reversed so the first section prints first.
        foreach (var s in Enumerable.Reverse(sections))
        {
            series.Add((double)s.Total);
            names.Add(s.Provisional ? $"{s.Name} (provisional)" : s.Name);
        }

        chart.XAxis.TickLabels.Font.Size = 8;
        chart.XAxis.MajorTickMark = TickMarkType.None;
        chart.XAxis.LineFormat.Color = Hair;
        chart.YAxis.TickLabels.Format = "#,##0";
        chart.YAxis.TickLabels.Font.Size = 7.5;
        chart.YAxis.HasMajorGridlines = true;
        chart.YAxis.MajorGridlines.LineFormat.Color = Hair;
        chart.YAxis.MajorTickMark = TickMarkType.None;
        chart.YAxis.LineFormat.Width = 0;
        chart.YAxis.MinimumScale = 0;
        // Headroom past the longest bar so its value label has somewhere to sit.
        chart.YAxis.MaximumScale = Math.Ceiling((double)sections.Max(s => s.Total) * 1.18 / 1000) * 1000;
        chart.DataLabel.Type = DataLabelType.Value;
        chart.DataLabel.Position = DataLabelPosition.OutsideEnd;
        chart.DataLabel.Format = "  £#,##0";
        chart.DataLabel.Font.Size = 7.5;
        chart.DataLabel.Font.Color = Ink;
    }

    // ---- The itemised sections ----------------------------------------------------------------

    private static void AddSections(Section section, LeadEstimate estimate)
    {
        var sections = estimate.Sections.Where(s => s.Lines.Count > 0).ToList();
        if (sections.Count == 0) return;

        section.AddPageBreak();
        foreach (var breakdownSection in sections)
        {
            var title = section.AddParagraph();
            title.Format.Font.Size = 14;
            title.Format.Font.Color = Ink;
            title.Format.KeepWithNext = true;
            title.AddText(breakdownSection.Name);
            if (breakdownSection.Provisional) title.AddFormattedText(" — Provisional allowance", new Font { Color = Provisional });
            SpaceBefore(title, 6);
            SpaceAfter(title, 2);

            var table = section.AddTable();
            table.Borders.Color = Hair;
            table.Borders.Width = 0.5;
            table.Format.Font.Size = 8.5;
            AddColumn(table, 2.7, ParagraphAlignment.Center);
            AddColumn(table, 6.5, ParagraphAlignment.Left);
            AddColumn(table, 1.8, ParagraphAlignment.Center);
            AddColumn(table, 1.8, ParagraphAlignment.Center);
            AddColumn(table, 2.5, ParagraphAlignment.Right);
            AddColumn(table, 2.5, ParagraphAlignment.Right);

            var header = table.AddRow();
            header.HeadingFormat = true;
            header.Shading.Color = Gold;
            header.TopPadding = Unit.FromMillimeter(1.6);
            header.BottomPadding = Unit.FromMillimeter(1.6);
            var headings = new[] { "ID", "Description", "Quantity", "Unit", "Unit price (£)", "Total (£)" };
            for (var i = 0; i < headings.Length; i++)
            {
                var paragraph = header.Cells[i].AddParagraph(headings[i]);
                paragraph.Format.Font.Color = White;
                paragraph.Format.Font.Size = 8.5;
                paragraph.Format.LeftIndent = CellIndent;
                paragraph.Format.RightIndent = CellIndent;
            }

            foreach (var line in breakdownSection.Lines)
            {
                var row = table.AddRow();
                row.TopPadding = Unit.FromMillimeter(1.4);
                row.BottomPadding = Unit.FromMillimeter(1.4);
                row.VerticalAlignment = VerticalAlignment.Center;
                Cell(row.Cells[0], line.CostCode);
                row.Cells[0].Format.Font.Size = 7.5;
                Cell(row.Cells[1], line.Description);
                Cell(row.Cells[2], Quantity(line.Quantity));
                Cell(row.Cells[3], line.Unit);
                Cell(row.Cells[4], Pence(line.UnitPrice));
                Cell(row.Cells[5], Pence(line.Total));
            }

            var totalRow = table.AddRow();
            totalRow.TopPadding = Unit.FromMillimeter(1.6);
            totalRow.BottomPadding = Unit.FromMillimeter(1.6);
            totalRow.Cells[4].Shading.Color = Orange;
            var label = totalRow.Cells[4].AddParagraph("Total cost:");
            label.Format.Font.Bold = true;
            label.Format.Font.Color = White;
            label.Format.Alignment = ParagraphAlignment.Center;
            var total = totalRow.Cells[5].AddParagraph(Pence(breakdownSection.Total));
            total.Format.Font.Bold = true;
            total.Format.RightIndent = CellIndent;

            SpaceAfterTable(section);
        }
    }

    private static void AddGrandTotal(Section section, LeadEstimate estimate)
    {
        var sections = estimate.Sections.Where(s => s.Lines.Count > 0).ToList();
        if (sections.Count == 0) return;

        var title = section.AddParagraph("Total project estimate value");
        title.Format.Font.Size = 14;
        title.Format.KeepWithNext = true;
        SpaceBefore(title, 8);
        SpaceAfter(title, 2);

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        AddColumn(table, 5.0, ParagraphAlignment.Left);
        AddColumn(table, 7.8, ParagraphAlignment.Right);
        AddColumn(table, 2.5, ParagraphAlignment.Right);
        AddColumn(table, 2.5, ParagraphAlignment.Right);

        var provisional = sections.Where(s => s.Provisional).Sum(s => s.Total);
        foreach (var s in sections)
        {
            var row = table.AddRow();
            row.TopPadding = Unit.FromMillimeter(1);
            row.BottomPadding = Unit.FromMillimeter(1);
            Cell(row.Cells[0], s.Name);
            Cell(row.Cells[1], s.Provisional ? "Provisional allowance" : "");
            if (s.Provisional) row.Cells[1].Format.Font.Color = Provisional;
            Cell(row.Cells[3], Pence(s.Total));
        }

        var totalRow = table.AddRow();
        totalRow.TopPadding = Unit.FromMillimeter(1.8);
        totalRow.BottomPadding = Unit.FromMillimeter(1.8);
        totalRow.Cells[2].Shading.Color = Orange;
        var label = totalRow.Cells[2].AddParagraph("Total cost:");
        label.Format.Font.Bold = true;
        label.Format.Font.Color = White;
        label.Format.Alignment = ParagraphAlignment.Center;
        var total = totalRow.Cells[3].AddParagraph(Pence(sections.Sum(s => s.Total)));
        total.Format.Font.Bold = true;
        total.Format.Font.Size = 10;
        total.Format.RightIndent = CellIndent;

        var note = section.AddParagraph(provisional > 0
            ? $"All figures exclude VAT. Provisional allowances ({Pence(provisional)}) are to be confirmed once the design and the site conditions are known."
            : "All figures exclude VAT.");
        note.Format.Font.Size = 8;
        note.Format.Font.Color = Muted;
        SpaceBefore(note, 2);
    }

    // ---- Contact ------------------------------------------------------------------------------

    private static void AddContactPage(Section section)
    {
        section.AddPageBreak();
        SectionTitle(section, "Contact us");
        var blurb = section.AddParagraph(
            "Reach out to us to discuss your upcoming projects. We can demonstrate how our expertise "
            + "perfectly matches your specific needs.");
        blurb.Format.Font.Size = 9.5;
        SpaceAfter(blurb, 3);
        var blurb2 = section.AddParagraph(
            "Experience the quality and craftsmanship we bring to every project by visiting one of our "
            + "current sites. Witness our dedication to excellence in construction firsthand. Feel free "
            + "to get in touch with us.");
        blurb2.Format.Font.Size = 9.5;
        SpaceAfter(blurb2, 5);

        ContactLine(section, "Call us:", "0208 109 1015");
        ContactLine(section, "Email us:", "sales@jewelbb.co.uk");
        ContactLine(section, "Website:", "www.jewelbb.co.uk");
        ContactLine(section, "Address:", "Jewel Bespoke Build, Argent House, Surbiton, KT6 7LD");
    }

    private static void ContactLine(Section section, string label, string value)
    {
        var heading = section.AddParagraph(label);
        heading.Format.Font.Bold = true;
        heading.Format.Font.Size = 9.5;
        heading.Format.KeepWithNext = true;
        var line = section.AddParagraph(value);
        line.Format.Font.Size = 9.5;
        SpaceAfter(line, 3);
    }

    // ---- Helpers ------------------------------------------------------------------------------

    private static void SectionTitle(Section section, string text)
    {
        var title = section.AddParagraph(text);
        title.Format.Font.Size = 16;
        title.Format.Font.Bold = true;
        title.Format.Font.Color = Ink;
        title.Format.KeepWithNext = true;
        SpaceAfter(title, 3);
    }

    private static void SubHeading(Section section, string text)
    {
        var heading = section.AddParagraph(text);
        heading.Format.Font.Size = 11;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = Navy;
        heading.Format.KeepWithNext = true;
        SpaceBefore(heading, 4);
        SpaceAfter(heading, 1.5);
    }

    private static void AddColumn(Table table, double widthCm, ParagraphAlignment alignment)
    {
        var column = table.AddColumn(Unit.FromCentimeter(widthCm));
        column.Format.Alignment = alignment;
    }

    private static void Cell(Cell cell, string text)
    {
        var paragraph = cell.AddParagraph(string.IsNullOrWhiteSpace(text) ? "" : text);
        paragraph.Format.LeftIndent = CellIndent;
        paragraph.Format.RightIndent = CellIndent;
        paragraph.Format.Font.Size = 8.5;
    }

    private static string PropertyLine(Lead lead)
    {
        var parts = new[] { lead.PropertyAddress, lead.Postcode }.Where(value => !string.IsNullOrWhiteSpace(value));
        var line = string.Join(", ", parts);
        if (!string.IsNullOrWhiteSpace(line)) return line;
        return string.IsNullOrWhiteSpace(lead.ContactName) ? lead.CompanyName : lead.ContactName;
    }

    private static string ClientLine(Lead lead)
    {
        if (!string.IsNullOrWhiteSpace(lead.CompanyName) && !string.IsNullOrWhiteSpace(lead.ContactName))
            return $"{lead.ContactName}, {lead.CompanyName}";
        return string.IsNullOrWhiteSpace(lead.ContactName) ? lead.CompanyName : lead.ContactName;
    }

    private static string WholeMoney(decimal value) => value.ToString("C0", Uk);
    private static string Pence(decimal value) => value.ToString("N2", Uk);
    private static string Quantity(decimal value) => value == decimal.Truncate(value) ? value.ToString("N0", Uk) : value.ToString("0.##", Uk);
}
