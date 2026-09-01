using System.Globalization;
using Jewel.JPMS.Api.Features.Requests.Documents;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Procurement.Documents;

public static partial class WorkOrderPoRenderer
{
    // ---- Sections -----------------------------------------------------------------------------

    private static void AddHeaderBand(Section section, WorkOrderPoDocumentModel model)
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

        DocumentBranding.AddLogo(row.Cells[0], Unit.FromCentimeter(3.4), Unit.FromMillimeter(1.5));

        var heading = row.Cells[0].AddParagraph("PURCHASE ORDER");
        heading.Format.Font.Size = 17;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = White;
        SpaceAfter(heading, 1);

        var sub = row.Cells[0].AddParagraph(string.IsNullOrWhiteSpace(model.Order.Title)
            ? model.SupplierName
            : model.Order.Title);
        sub.Format.Font.Size = 9.5;
        sub.Format.Font.Bold = true;
        sub.Format.Font.Color = Gold;

        var stamp = row.Cells[1].AddParagraph(model.Order.Reference);
        stamp.Format.Font.Size = 13;
        stamp.Format.Font.Bold = true;
        stamp.Format.Font.Color = White;
        SpaceAfter(stamp, 2);

        // Company block — the sheet's top-right identity, letter-style.
        foreach (var line in new[]
                 {
                     "Jewel Bespoke Build Ltd",
                     "Argent House, 175 Hook Rise South,",
                     "Surbiton, Greater London, KT6 7LD",
                     "Phone: 0208 109 1014"
                 })
        {
            var p = row.Cells[1].AddParagraph(line);
            p.Format.Font.Size = 7.5;
            p.Format.Font.Color = Gold;
        }

        Hairline(section);
    }

    private static void AddDetailsGrid(Section section, WorkOrderPoDocumentModel model)
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

        // "— on approval": a draft has not been released, so AwardedAt still holds the drafting
        // stamp — printing it would date a release that hasn't happened (same rule as the sheet).
        AddGridRow(table,
            "Purchase order #", model.Order.Reference,
            "Total price", Money(model.Order.Value));
        AddGridRow(table,
            "Date created", Date(model.Order.CreatedAt),
            "Date released", model.Order.IsDraft ? "— on approval" : Date(model.Order.AwardedAt));
        AddGridRow(table,
            "Scheduled completion",
            model.Order.ScheduledCompletion is { } completion ? Date(completion) : "—",
            "Payment terms", $"{model.PaymentTermsDays} days");
        SpaceAfterTable(section);
    }

    private static void AddParties(Section section, WorkOrderPoDocumentModel model)
    {
        var table = section.AddTable();
        table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(8.9));
        table.AddColumn(Unit.FromCentimeter(8.9));

        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.5);
        row.BottomPadding = Unit.FromMillimeter(1);

        PartyBlock(row.Cells[0], "SUB / VENDOR", model.SupplierName,
            new[] { model.SupplierContactName }.Concat(model.SupplierAddressLines));
        PartyBlock(row.Cells[1], "JOB", model.ProjectName, model.SiteAddressLines);
        SpaceAfterTable(section);
    }

    private static void PartyBlock(Cell cell, string label, string name, IEnumerable<string> lines)
    {
        var heading = cell.AddParagraph(label);
        heading.Format.Font.Size = 7.5;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = Orange;
        SpaceAfter(heading, 1);

        var strong = cell.AddParagraph(string.IsNullOrWhiteSpace(name) ? "—" : name);
        strong.Format.Font.Size = 9.5;
        strong.Format.Font.Bold = true;
        strong.Format.Font.Color = Navy;

        foreach (var line in lines.Where(part => !string.IsNullOrWhiteSpace(part)))
        {
            var p = cell.AddParagraph(line);
            p.Format.Font.Size = 8.5;
        }
    }

    private static void AddSummaryTable(Section section, WorkOrderPoDocumentModel model)
    {
        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(9.8));
        table.AddColumn(Unit.FromCentimeter(4.0));
        var price = table.AddColumn(Unit.FromCentimeter(4.0));
        price.Format.Alignment = ParagraphAlignment.Right;

        var header = table.AddRow();
        header.Shading.Color = Panel;
        header.TopPadding = Unit.FromMillimeter(1.2);
        header.BottomPadding = Unit.FromMillimeter(1.2);
        header.HeadingFormat = true;
        HeaderCell(header.Cells[0], "PO Title");
        HeaderCell(header.Cells[1], "Scheduled Completion");
        HeaderCell(header.Cells[2], "Total Price");

        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.4);
        row.BottomPadding = Unit.FromMillimeter(1.4);
        var title = row.Cells[0].AddParagraph(string.IsNullOrWhiteSpace(model.Order.Title)
            ? model.SupplierName
            : model.Order.Title);
        title.Format.LeftIndent = Unit.FromMillimeter(1.5);
        title.Format.Font.Size = 9;
        title.Format.Font.Bold = true;
        var when = row.Cells[1].AddParagraph(model.Order.ScheduledCompletion is { } completion
            ? Date(completion)
            : "—");
        when.Format.LeftIndent = Unit.FromMillimeter(1.5);
        when.Format.Font.Size = 9;
        MoneyCell(row.Cells[2], model.Order.Value, bold: true);
        SpaceAfterTable(section);
    }

    private static void AddScopeOfWork(Section section, WorkOrderPoDocumentModel model)
    {
        SectionHeading(section, "Scope of Work");

        var quote = section.AddParagraph(
            "“This Works Order is issued subject to the terms and conditions which are appended hereto”");
        quote.Format.Font.Size = 8.5;
        quote.Format.Font.Italic = true;
        quote.Format.Font.Color = Muted;
        SpaceAfter(quote, 1.5);

        // The terms live on the public site; browser-printed sheets carry a live link, so the PDF
        // prints the address itself for the supplier to follow.
        var terms = section.AddParagraph("Terms of work order: ");
        terms.Format.Font.Size = 8.5;
        terms.AddFormattedText("https://www.jewelbb.co.uk/copy-of-privacy-policy",
            new Font { Color = Gold, Bold = true, Size = 8.5 });
        SpaceAfter(terms, 2);

        SubHeading(section, "Special Instructions");
        BodyText(section,
            "Every site requires full PPE, boots and Hi-Vis, please ensure these are worn at all times, "
            + "with all RAMS adhered to.");
        BodyText(section,
            "We expect all of your work areas to be clean and tidy and left safe at the end of each "
            + "working day.");
        BodyText(section,
            "Ensure you are representing Jewel Bespoke Build by being polite and courteous to all "
            + "clients and neighbours at all times.");

        SubHeading(section, "Insurances & RAMS");
        BodyText(section,
            "Contractors to send all insurance documents and RAMS prior to starting works on site — "
            + "projects@jewelbb.co.uk");

        if (!string.IsNullOrWhiteSpace(model.Order.Scope))
        {
            SubHeading(section, "Works Order info");
            PrewrapText(section, model.Order.Scope);
        }

        var hasProgramme = model.Order.ProgrammeStart is not null
            || model.Order.ScheduledCompletion is not null
            || !string.IsNullOrWhiteSpace(model.Order.ProgrammeNotes);
        if (hasProgramme)
        {
            SubHeading(section, "Programme");
            if (model.Order.ProgrammeStart is { } start)
                BodyText(section, $"Start Date — {Date(start)}");
            if (model.Order.ScheduledCompletion is { } completion)
                BodyText(section, $"Completion Date — {Date(completion)}");
            if (!string.IsNullOrWhiteSpace(model.Order.ProgrammeNotes))
                PrewrapText(section, model.Order.ProgrammeNotes);
        }

        BodyText(section,
            "All changes to the contract sum (variations) must be notified in writing, and written "
            + "approval to be received prior to any additional works being completed. Any works "
            + "completed without written authorisation cannot be charged.");

        SubHeading(section, "Invoice and Payment Requirements");
        BodyText(section,
            "Please forward your invoice to accounts@jewelbb.co.uk and projects@jewelbb.co.uk by COB "
            + $"Friday's with a {model.PaymentTermsDays} day terms");
        BodyText(section, "Invoices to include CIS Breakdown (if necessary)");
        BodyText(section, "Correct VAT breakdown (including Reverse Charge if necessary)");
        BodyText(section,
            "In the event that your invoice does not contain the correct info or sent in on time, we "
            + "will be unable to process it in a timely manner.");

        // Deposit — only when the order both requires one and carries the percentage; the flag
        // without a figure would print an empty promise (same rule as the sheet).
        if (model.Order is { DepositRequired: true, DepositPercent: { } percent })
        {
            SubHeading(section, "Deposit");
            BodyText(section,
                $"A deposit of {percent.ToString("0.##", Uk)}% of the order value is required on this order.");
        }

        BodyText(section, "We thank you for your business and look forward to working with you.");
        BodyText(section,
            "Jewel Bespoke Build Ltd is Incorporated in England with Limited Liability. "
            + "Registered Company Number: 13752749");
    }

    private static void AddLinesTable(Section section, WorkOrderPoDocumentModel model)
    {
        if (model.Lines.Count == 0) return;

        SectionHeading(section, "Order Lines");

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(3.4));                              // item + cost code
        table.AddColumn(Unit.FromCentimeter(2.0));                              // cost type
        table.AddColumn(Unit.FromCentimeter(4.6));                              // description
        var qty = table.AddColumn(Unit.FromCentimeter(1.9));
        var unitCost = table.AddColumn(Unit.FromCentimeter(2.0));
        var priceCol = table.AddColumn(Unit.FromCentimeter(2.0));
        var paidCol = table.AddColumn(Unit.FromCentimeter(1.9));
        qty.Format.Alignment = ParagraphAlignment.Right;
        unitCost.Format.Alignment = ParagraphAlignment.Right;
        priceCol.Format.Alignment = ParagraphAlignment.Right;
        paidCol.Format.Alignment = ParagraphAlignment.Right;

        var header = table.AddRow();
        header.Shading.Color = Panel;
        header.TopPadding = Unit.FromMillimeter(1.2);
        header.BottomPadding = Unit.FromMillimeter(1.2);
        header.HeadingFormat = true;
        HeaderCell(header.Cells[0], "Items");
        HeaderCell(header.Cells[1], "Cost Types");
        HeaderCell(header.Cells[2], "Description");
        HeaderCell(header.Cells[3], "Qty/Unit");
        HeaderCell(header.Cells[4], "Unit Cost");
        HeaderCell(header.Cells[5], "Price");
        HeaderCell(header.Cells[6], "Paid");

        foreach (var line in model.Lines.OrderBy(line => line.SortOrder))
        {
            var row = table.AddRow();
            row.TopPadding = Unit.FromMillimeter(1.2);
            row.BottomPadding = Unit.FromMillimeter(1.2);

            var item = row.Cells[0].AddParagraph();
            item.Format.LeftIndent = Unit.FromMillimeter(1.5);
            item.Format.Font.Size = 8.5;
            item.AddFormattedText(line.Title, new Font { Bold = true });
            if (!string.IsNullOrWhiteSpace(line.CostCode))
                item.AddFormattedText($"  · {line.CostCode}", new Font { Color = Muted, Size = 7.5 });

            var type = row.Cells[1].AddParagraph(string.IsNullOrWhiteSpace(line.CostType) ? "—" : line.CostType);
            type.Format.LeftIndent = Unit.FromMillimeter(1.5);
            type.Format.Font.Size = 8;
            type.Format.Font.Color = Muted;

            // Pre-wrap: a multi-line description keeps its typed line breaks, same as the sheet.
            PrewrapCell(row.Cells[2], line.Description);

            var quantity = row.Cells[3].AddParagraph(
                $"{line.Quantity.ToString("0.##", Uk)} {line.Unit}".Trim());
            quantity.Format.RightIndent = Unit.FromMillimeter(1.5);
            quantity.Format.Font.Size = 8.5;

            MoneyCell(row.Cells[4], line.UnitCost);
            MoneyCell(row.Cells[5], line.LineTotal);
            if (line.PaidToDate == 0m)
            {
                var dash = row.Cells[6].AddParagraph("–");
                dash.Format.RightIndent = Unit.FromMillimeter(1.5);
                dash.Format.Font.Size = 8.5;
                dash.Format.Font.Color = Muted;
            }
            else
            {
                MoneyCell(row.Cells[6], line.PaidToDate);
            }
        }

        var totalPaid = model.Lines.Sum(line => line.PaidToDate);
        var totals = table.AddRow();
        totals.Shading.Color = Panel;
        totals.TopPadding = Unit.FromMillimeter(1.4);
        totals.BottomPadding = Unit.FromMillimeter(1.4);
        var label = totals.Cells[2].AddParagraph("Totals");
        label.Format.LeftIndent = Unit.FromMillimeter(1.5);
        label.Format.Font.Size = 8.5;
        label.Format.Font.Bold = true;
        label.Format.Font.Color = Navy;
        MoneyCell(totals.Cells[5], model.Lines.Sum(line => line.LineTotal), bold: true);
        MoneyCell(totals.Cells[6], totalPaid, bold: true);

        var remaining = section.AddParagraph();
        remaining.Format.Alignment = ParagraphAlignment.Right;
        remaining.Format.Font.Size = 9;
        SpaceBefore(remaining, 1.5);
        remaining.AddFormattedText("Remaining Balance:  ", new Font { Bold = true, Color = Navy });
        remaining.AddFormattedText(Money(model.Order.Value - totalPaid), new Font { Bold = true });
        SpaceAfter(remaining, 2);
    }

    private static void AddAcceptanceWording(Section section)
    {
        var legal = section.AddParagraph(
            "A signature of Approval or Electronic Acceptance is required before this purchase order is "
            + "effective. This purchase order then becomes part of the existing contract and is binding "
            + "and subject to our terms and conditions detailed in our Work Orders.");
        legal.Format.Font.Size = 8;
        legal.Format.Font.Color = Muted;
        SpaceBefore(legal, 3);
        SpaceAfter(legal, 3);
    }

    private static void AddSignatures(Section section, WorkOrderPoDocumentModel model)
    {
        var table = section.AddTable();
        table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(8.9));
        table.AddColumn(Unit.FromCentimeter(8.9));

        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(2);

        // Approval side — a draft has nobody to name (same empty-line rule as the sheet), but the
        // reply-draft flow refuses drafts, so in practice this always prints the approver.
        if (model.Order.IsDraft)
        {
            SignatureBlock(row.Cells[0], "", "Awaiting approval", "");
        }
        else
        {
            var approver = string.IsNullOrWhiteSpace(model.ApprovedByName)
                ? model.Order.AwardedByEmail
                : model.ApprovedByName;
            SignatureBlock(row.Cells[0], approver, "Approved by", DateTime(model.Order.AwardedAt));
        }

        if (model.Order.IsAccepted)
        {
            var acceptor = string.IsNullOrWhiteSpace(model.Order.AcceptedByName)
                ? model.Order.AcceptedByEmail
                : model.Order.AcceptedByName;
            SignatureBlock(row.Cells[1], acceptor, "Electronically accepted by",
                DateTime(model.Order.AcceptedAt!.Value));
        }
        else
        {
            SignatureBlock(row.Cells[1], "", "Awaiting electronic acceptance", "");
        }
    }

    private static void SignatureBlock(Cell cell, string name, string label, string when)
    {
        var nameLine = cell.AddParagraph(string.IsNullOrWhiteSpace(name) ? " " : name);
        nameLine.Format.Font.Size = 10;
        nameLine.Format.Font.Bold = true;
        nameLine.Format.Font.Color = Navy;
        nameLine.Format.Borders.Bottom.Width = 0.75;
        nameLine.Format.Borders.Bottom.Color = Hair;
        nameLine.Format.Borders.Distance = Unit.FromMillimeter(1.5);
        nameLine.Format.RightIndent = Unit.FromCentimeter(1.5);
        SpaceAfter(nameLine, 1);

        var labelLine = cell.AddParagraph(label);
        labelLine.Format.Font.Size = 7.5;
        labelLine.Format.Font.Bold = true;
        labelLine.Format.Font.Color = Muted;

        var whenLine = cell.AddParagraph(string.IsNullOrWhiteSpace(when) ? " " : when);
        whenLine.Format.Font.Size = 8;
        whenLine.Format.Font.Color = Muted;
    }

    private static void AddFooter(Section section)
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
            $"Generated {DateTime(DateTimeOffset.Now)} · from the JPMS register (source of truth)",
            new Font { Color = Muted, Size = 7 });

        footer.Format.TabStops.AddTabStop(Unit.FromCentimeter(18.3), TabAlignment.Right);
    }
}
