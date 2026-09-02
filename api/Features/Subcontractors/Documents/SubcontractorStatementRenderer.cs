using System.Globalization;
using Jewel.JPMS.Api.Features.Requests.Documents;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

using Jewel.JPMS.Api.Features.Documents;
using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Subcontractors.Documents;

/// <summary>
/// Renders a <see cref="SubcontractorStatement"/> into a branded statement-of-account PDF using
/// PDFsharp/MigraDoc: the subcontractor's work orders grouped by project, each with the invoices
/// claimed against it, plus per-project and overall balances. Pure function of the statement (the
/// GeneratedAt stamp travels in the model), so download and email attachment render identically.
/// Follows the JewelBB palette established by <see cref="Progress.Documents.ProgressReportRenderer"/>.
/// </summary>
public static class SubcontractorStatementRenderer
{
    private static readonly Color Negative = new(0xB4, 0x23, 0x18);



    public static byte[] Render(SubcontractorStatement statement)
    {
        EnsureFonts();

        var document = new Document();
        document.Info.Title = $"{statement.CompanyName} Statement of Account".Trim();
        document.Info.Author = "Jewel Bespoke Build";
        document.Info.Subject = "Statement of account";

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

        AddHeaderBand(section, statement);
        AddDetailsGrid(section, statement);

        if (statement.Projects.Count == 0)
        {
            SectionHeading(section, "Account");
            var none = section.AddParagraph("No work orders are recorded on this account.");
            none.Format.Font.Size = 9.5;
            none.Format.Font.Color = Muted;
        }

        foreach (var project in statement.Projects)
            AddProject(section, project);

        AddClosingNote(section);
        AddFooter(section, statement);

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    // ---- Sections -----------------------------------------------------------------------------

    private static void AddHeaderBand(Section section, SubcontractorStatement statement)
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

        // The official Jewel Bespoke Build logo leads the band — the gold/orange registered
        // artwork reads directly on the navy ground (embedded once in DocumentBranding).
        DocumentBranding.AddLogo(row.Cells[0], Unit.FromCentimeter(3.4), Unit.FromMillimeter(1.5));

        var heading = row.Cells[0].AddParagraph("STATEMENT OF ACCOUNT");
        heading.Format.Font.Size = 17;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = White;
        SpaceAfter(heading, 1);

        var sub = row.Cells[0].AddParagraph(statement.CompanyName);
        sub.Format.Font.Size = 9.5;
        sub.Format.Font.Bold = true;
        sub.Format.Font.Color = Gold;

        var stamp = row.Cells[1].AddParagraph("SUBCONTRACTOR ACCOUNT");
        stamp.Format.Font.Size = 10;
        stamp.Format.Font.Bold = true;
        stamp.Format.Font.Color = White;
        SpaceAfter(stamp, 2);

        var date = row.Cells[1].AddParagraph($"Statement date  {Date(statement.GeneratedAt)}");
        date.Format.Font.Size = 8;
        date.Format.Font.Color = Gold;

        Hairline(section);
    }

    private static void AddDetailsGrid(Section section, SubcontractorStatement statement)
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
            "Company", statement.CompanyName,
            "Statement date", Date(statement.GeneratedAt));
        AddGridRow(table,
            "Contact", statement.ContactName,
            "Work orders", statement.OrderCount.ToString(Uk));
        AddGridRow(table,
            "Total ordered", Money(statement.TotalOrdered),
            "Invoiced to date", Money(statement.TotalInvoiced));
        AddGridRow(table,
            "Remaining to invoice", Money(statement.TotalRemaining),
            "Projects", statement.Projects.Count.ToString(Uk));

        SpaceAfterTable(section);
    }

    private static void AddProject(Section section, SubcontractorStatementProject project)
    {
        SectionHeading(section, string.IsNullOrWhiteSpace(project.ProjectReference)
            ? project.ProjectName
            : $"{project.ProjectReference} — {project.ProjectName}");

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(2.0));                              // order / invoice ref
        table.AddColumn(Unit.FromCentimeter(6.1));                              // title / invoice detail
        var status = table.AddColumn(Unit.FromCentimeter(2.2));                 // status / invoice date
        var value = table.AddColumn(Unit.FromCentimeter(2.5));
        var invoiced = table.AddColumn(Unit.FromCentimeter(2.5));
        var remaining = table.AddColumn(Unit.FromCentimeter(2.5));
        value.Format.Alignment = ParagraphAlignment.Right;
        invoiced.Format.Alignment = ParagraphAlignment.Right;
        remaining.Format.Alignment = ParagraphAlignment.Right;

        var header = table.AddRow();
        header.Shading.Color = Panel;
        header.TopPadding = Unit.FromMillimeter(1.2);
        header.BottomPadding = Unit.FromMillimeter(1.2);
        header.HeadingFormat = true;
        HeaderCell(header.Cells[0], "Order");
        HeaderCell(header.Cells[1], "Description");
        HeaderCell(header.Cells[2], "Status");
        HeaderCell(header.Cells[3], "Order value");
        HeaderCell(header.Cells[4], "Invoiced");
        HeaderCell(header.Cells[5], "Remaining");

        foreach (var order in project.Orders)
        {
            var row = table.AddRow();
            row.TopPadding = Unit.FromMillimeter(1.4);
            row.BottomPadding = Unit.FromMillimeter(1.4);

            var reference = row.Cells[0].AddParagraph(order.Reference);
            reference.Format.Font.Size = 8.5;
            reference.Format.Font.Bold = true;
            reference.Format.Font.Color = Navy;

            var title = row.Cells[1].AddParagraph(string.IsNullOrWhiteSpace(order.Title) ? "—" : order.Title);
            title.Format.Font.Size = 9;
            title.Format.Font.Bold = true;

            var state = row.Cells[2].AddParagraph(StatusLabel(order.Status));
            state.Format.Font.Size = 8;
            state.Format.Font.Color = Muted;

            MoneyCell(row.Cells[3], order.Value, bold: true);
            MoneyCell(row.Cells[4], order.InvoicedToDate, bold: true);
            MoneyCell(row.Cells[5], order.RemainingToInvoice, bold: true,
                colour: order.RemainingToInvoice < 0 ? Negative : null);

            if (order.Invoices.Count == 0)
            {
                var empty = table.AddRow();
                empty.TopPadding = Unit.FromMillimeter(0.8);
                empty.BottomPadding = Unit.FromMillimeter(0.8);
                var note = empty.Cells[1].AddParagraph("No invoices claimed against this order yet.");
                note.Format.Font.Size = 8;
                note.Format.Font.Italic = true;
                note.Format.Font.Color = Muted;
                continue;
            }

            foreach (var invoice in order.Invoices)
            {
                var line = table.AddRow();
                line.TopPadding = Unit.FromMillimeter(0.8);
                line.BottomPadding = Unit.FromMillimeter(0.8);

                var marker = line.Cells[0].AddParagraph(invoice.IsCreditNote ? "Credit note" : "Invoice");
                marker.Format.Font.Size = 7.5;
                marker.Format.Font.Color = Muted;
                marker.Format.LeftIndent = Unit.FromMillimeter(3);

                var detailText = invoice.InvoiceNumber;
                if (!string.IsNullOrWhiteSpace(invoice.InvoiceReference)
                    && !string.Equals(invoice.InvoiceReference, invoice.InvoiceNumber, StringComparison.OrdinalIgnoreCase))
                    detailText += $"  ·  {invoice.InvoiceReference}";
                var detail = line.Cells[1].AddParagraph(detailText);
                detail.Format.Font.Size = 8.5;
                detail.Format.LeftIndent = Unit.FromMillimeter(3);

                var when = line.Cells[2].AddParagraph(invoice.Date is { } date ? Date(date) : "—");
                when.Format.Font.Size = 8;
                when.Format.Font.Color = Muted;

                MoneyCell(line.Cells[4], invoice.Amount,
                    colour: invoice.IsCreditNote ? Negative : null);
            }
        }

        var totals = table.AddRow();
        totals.Shading.Color = Panel;
        totals.TopPadding = Unit.FromMillimeter(1.4);
        totals.BottomPadding = Unit.FromMillimeter(1.4);
        var label = totals.Cells[1].AddParagraph("Project total");
        label.Format.Font.Size = 8.5;
        label.Format.Font.Bold = true;
        label.Format.Font.Color = Navy;
        MoneyCell(totals.Cells[3], project.Ordered, bold: true);
        MoneyCell(totals.Cells[4], project.Invoiced, bold: true);
        MoneyCell(totals.Cells[5], project.Remaining, bold: true,
            colour: project.Remaining < 0 ? Negative : null);

        SpaceAfterTable(section);
    }

    private static void AddClosingNote(Section section)
    {
        var note = section.AddParagraph(
            "All figures are net of VAT. Invoiced amounts are the shares of your invoices allocated against "
            + "each work order; credit notes are shown as deductions. If anything on this statement doesn't "
            + "match your records, please get in touch so we can reconcile it together.");
        note.Format.Font.Size = 8;
        note.Format.Font.Color = Muted;
        SpaceBefore(note, 2);
    }

    private static void AddFooter(Section section, SubcontractorStatement statement)
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
            $"Generated {DateTime(statement.GeneratedAt)} · from the JPMS register (source of truth)",
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

    private static void HeaderCell(Cell cell, string text)
    {
        cell.Format.LeftIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(text);
        p.Format.Font.Size = 7.5;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Muted;
    }

    private static void MoneyCell(Cell cell, decimal amount, bool bold = false, Color? colour = null)
    {
        cell.Format.RightIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(Money(amount));
        p.Format.Font.Size = 8.5;
        p.Format.Font.Bold = bold;
        p.Format.Font.Color = colour ?? Ink;
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


    private static void SpaceBefore(Paragraph p, double mm) => p.Format.SpaceBefore = Unit.FromMillimeter(mm);
    private static void SpaceAfter(Paragraph p, double mm) => p.Format.SpaceAfter = Unit.FromMillimeter(mm);


    private static string StatusLabel(WorkOrderStatus status) => status switch
    {
        WorkOrderStatus.Draft => "Draft",
        WorkOrderStatus.Released => "Released",
        WorkOrderStatus.Complete => "Complete",
        WorkOrderStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    // U+2212 MINUS SIGN, not the hyphen-minus: MigraDoc breaks a line after a hyphen that is not
    // followed by a digit, so "-£1,000.00" could print as a bare "-" with the figure on the next
    // line (the valuation report did exactly that, 2026-09-02). Same rule as
    // ValuationReportSnapshotRenderer.Money.
    private static string Money(decimal value) => value.ToString("£#,##0.00;\u2212£#,##0.00", Uk);
    private static string Date(System.DateTime value) => JewelDocumentStyle.Date(value);
    private static string Date(DateTimeOffset value) => JewelDocumentStyle.Date(value);
    private static string DateTime(DateTimeOffset value) => value.ToString("dd MMM yyyy HH:mm", Uk);

}
