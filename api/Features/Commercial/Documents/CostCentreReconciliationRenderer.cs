using System.Globalization;
using Jewel.JPMS.Api.Features.Requests.Documents;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

using static Jewel.JPMS.Api.Features.Documents.JewelDocumentStyle;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

/// <summary>
/// Everything the cost-centre reconciliation PDF needs: the project identity for the header,
/// the centre (or roll-up group) being reconciled, and the already-computed detail. Assembled
/// by <see cref="CostCentreReconciliationPdfBuilder"/>; the derived figures live here so the
/// PDF and the on-screen modal share one set of definitions.
/// </summary>
public sealed record CostCentreReconciliationDocument(
    string ProjectReference,
    string ProjectName,
    string ClientName,
    string Heading,
    IReadOnlyList<string> CostCodes,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<ReconciliationSalesLine> SalesLines,
    // Bridge: BudgetedSales less the listed lines (rounding, unlisted adjustments). Zero almost always.
    decimal SalesOther,
    IReadOnlyList<ReconciliationWorkOrderLine> WorkOrders,
    IReadOnlyList<ReconciliationCostLine> XeroCosts,
    decimal LabourCost,
    // Bridge: NonWorkOrderActualCost less labour less the listed Xero lines (splits, re-attributions).
    decimal OtherAdjustments,
    decimal SalesValue,
    decimal TargetCost,
    decimal WoCommitted,
    decimal NonWoCost)
{
    public decimal TotalCosts => WoCommitted + NonWoCost;
    public decimal GrossProfit => SalesValue - TotalCosts;
    /// <summary>Buying gain: what the scope was budgeted to cost less what it is costing.</summary>
    public decimal ProcurementGainLoss => TargetCost - TotalCosts;
    public decimal? MarginPercent => SalesValue == 0m ? null : Math.Round(GrossProfit / SalesValue * 100m, 1);
}

public sealed record ReconciliationSalesLine(string Reference, string Description, decimal Amount);

public sealed record ReconciliationWorkOrderLine(
    string Reference, string Supplier, string Title, string Status, decimal Amount);

public sealed record ReconciliationCostLine(
    string Date, string Supplier, string InvoiceNumber, string Description, decimal Amount);

/// <summary>
/// Renders one cost centre's reconciliation into a branded PDF using PDFsharp/MigraDoc — the
/// delivery position of a centre for the accountant to brief the managing director: the sales
/// lines grouped under the centre, the work orders (drafts included, marked) and Xero costs
/// against it, then gross profit, procurement gain / loss and margin. Pure function of the
/// document model, so the endpoint and any future email attachment render identically.
/// Follows the JewelBB palette established by ProgressReportRenderer.
/// </summary>
public static class CostCentreReconciliationRenderer
{
    private static readonly Color Negative = new(0xB4, 0x23, 0x18);



    public static byte[] Render(CostCentreReconciliationDocument document)
    {
        EnsureFonts();

        var pdf = new Document();
        pdf.Info.Title = $"{document.ProjectName} — {document.Heading} reconciliation".Trim();
        pdf.Info.Author = "Jewel Bespoke Build";
        pdf.Info.Subject = "Cost centre reconciliation";

        var normal = pdf.Styles["Normal"]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = 9;
        normal.Font.Color = Ink;

        var section = pdf.AddSection();
        var setup = section.PageSetup;
        setup.PageFormat = PageFormat.A4;
        setup.TopMargin = Unit.FromCentimeter(1.3);
        setup.BottomMargin = Unit.FromCentimeter(1.6);
        setup.LeftMargin = Unit.FromCentimeter(1.6);
        setup.RightMargin = Unit.FromCentimeter(1.6);

        AddHeaderBand(section, document);
        AddDetailsGrid(section, document);
        AddSalesLines(section, document);
        AddWorkOrders(section, document);
        AddXeroCosts(section, document);
        AddSummary(section, document);
        AddClosingNote(section, document);
        AddFooter(section, document);

        var renderer = new PdfDocumentRenderer { Document = pdf };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    // ---- Sections -----------------------------------------------------------------------------

    private static void AddHeaderBand(Section section, CostCentreReconciliationDocument document)
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

        var heading = row.Cells[0].AddParagraph("COST CENTRE RECONCILIATION");
        heading.Format.Font.Size = 17;
        heading.Format.Font.Bold = true;
        heading.Format.Font.Color = White;
        SpaceAfter(heading, 1);

        var sub = row.Cells[0].AddParagraph(string.IsNullOrWhiteSpace(document.ProjectReference)
            ? document.ProjectName
            : $"{document.ProjectReference} — {document.ProjectName}");
        sub.Format.Font.Size = 9.5;
        sub.Format.Font.Bold = true;
        sub.Format.Font.Color = Gold;

        var stamp = row.Cells[1].AddParagraph(document.Heading.ToUpperInvariant());
        stamp.Format.Font.Size = 10;
        stamp.Format.Font.Bold = true;
        stamp.Format.Font.Color = White;
        SpaceAfter(stamp, 2);

        var date = row.Cells[1].AddParagraph($"Generated  {DateTime(document.GeneratedAt)}");
        date.Format.Font.Size = 8;
        date.Format.Font.Color = Gold;

        Hairline(section);
    }

    private static void AddDetailsGrid(Section section, CostCentreReconciliationDocument document)
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
            "Project", document.ProjectName,
            "Client", document.ClientName);
        AddGridRow(table,
            "Cost centre", document.Heading,
            "Centre codes", string.Join(", ", document.CostCodes));
        AddGridRow(table,
            "Sales value", Money(document.SalesValue),
            "Target cost", Money(document.TargetCost));

        SpaceAfterTable(section);
    }

    private static void AddSalesLines(Section section, CostCentreReconciliationDocument document)
    {
        SectionHeading(section, "Sales — valuation lines under this centre");

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(2.0));                              // ref
        table.AddColumn(Unit.FromCentimeter(12.6));                             // description
        var amount = table.AddColumn(Unit.FromCentimeter(3.2));
        amount.Format.Alignment = ParagraphAlignment.Right;

        var header = table.AddRow();
        header.Shading.Color = Panel;
        HeaderCell(header.Cells[0], "REF");
        HeaderCell(header.Cells[1], "DESCRIPTION");
        HeaderCell(header.Cells[2], "AMOUNT");

        foreach (var line in document.SalesLines)
            AddLineRow(table, line.Reference, line.Description, line.Amount);
        if (document.SalesOther != 0m)
            AddLineRow(table, "", "Other / unlisted adjustments", document.SalesOther, muted: true);
        if (document.SalesLines.Count == 0 && document.SalesOther == 0m)
            AddEmptyRow(table, 3, "No valuation lines are coded to this centre.");

        AddTotalRow(table, 2, "Sales value", document.SalesValue);
        SpaceAfterTable(section);
    }

    private static void AddWorkOrders(Section section, CostCentreReconciliationDocument document)
    {
        SectionHeading(section, "Costs — work orders");

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(2.0));                              // ref
        table.AddColumn(Unit.FromCentimeter(4.6));                              // supplier
        table.AddColumn(Unit.FromCentimeter(6.2));                              // title
        table.AddColumn(Unit.FromCentimeter(1.8));                              // status
        var amount = table.AddColumn(Unit.FromCentimeter(3.2));
        amount.Format.Alignment = ParagraphAlignment.Right;

        var header = table.AddRow();
        header.Shading.Color = Panel;
        HeaderCell(header.Cells[0], "REF");
        HeaderCell(header.Cells[1], "SUPPLIER");
        HeaderCell(header.Cells[2], "ORDER");
        HeaderCell(header.Cells[3], "STATUS");
        HeaderCell(header.Cells[4], "THIS CENTRE");

        foreach (var order in document.WorkOrders)
        {
            var row = table.AddRow();
            row.TopPadding = Unit.FromMillimeter(1);
            row.BottomPadding = Unit.FromMillimeter(1);
            TextCell(row.Cells[0], order.Reference, mutedMono: true);
            TextCell(row.Cells[1], order.Supplier);
            TextCell(row.Cells[2], order.Title);
            TextCell(row.Cells[3], order.Status, mutedMono: true);
            MoneyCell(row.Cells[4], order.Amount);
        }
        if (document.WorkOrders.Count == 0)
            AddEmptyRow(table, 5, "No work orders are coded to this centre.");

        AddTotalRow(table, 4, "Work orders committed (drafts included)", document.WoCommitted);
        SpaceAfterTable(section);
    }

    private static void AddXeroCosts(Section section, CostCentreReconciliationDocument document)
    {
        SectionHeading(section, "Costs — Xero spend not on work orders");

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(2.2));                              // date
        table.AddColumn(Unit.FromCentimeter(4.6));                              // supplier
        table.AddColumn(Unit.FromCentimeter(2.4));                              // invoice
        table.AddColumn(Unit.FromCentimeter(5.4));                              // description
        var amount = table.AddColumn(Unit.FromCentimeter(3.2));
        amount.Format.Alignment = ParagraphAlignment.Right;

        var header = table.AddRow();
        header.Shading.Color = Panel;
        HeaderCell(header.Cells[0], "DATE");
        HeaderCell(header.Cells[1], "SUPPLIER");
        HeaderCell(header.Cells[2], "INVOICE");
        HeaderCell(header.Cells[3], "DESCRIPTION");
        HeaderCell(header.Cells[4], "AMOUNT");

        foreach (var line in document.XeroCosts)
        {
            var row = table.AddRow();
            row.TopPadding = Unit.FromMillimeter(1);
            row.BottomPadding = Unit.FromMillimeter(1);
            TextCell(row.Cells[0], line.Date, mutedMono: true);
            TextCell(row.Cells[1], line.Supplier);
            TextCell(row.Cells[2], line.InvoiceNumber, mutedMono: true);
            TextCell(row.Cells[3], line.Description);
            MoneyCell(row.Cells[4], line.Amount);
        }
        if (document.LabourCost != 0m)
        {
            var row = table.AddRow();
            row.TopPadding = Unit.FromMillimeter(1);
            row.BottomPadding = Unit.FromMillimeter(1);
            TextCell(row.Cells[0], "");
            TextCell(row.Cells[1], "Internal labour");
            TextCell(row.Cells[2], "");
            TextCell(row.Cells[3], "Approved timesheets coded to this centre");
            MoneyCell(row.Cells[4], document.LabourCost);
        }
        if (document.OtherAdjustments != 0m)
        {
            var row = table.AddRow();
            row.TopPadding = Unit.FromMillimeter(1);
            row.BottomPadding = Unit.FromMillimeter(1);
            TextCell(row.Cells[0], "");
            TextCell(row.Cells[1], "Other allocations & adjustments");
            TextCell(row.Cells[2], "");
            TextCell(row.Cells[3], "Splits and re-attributions not itemised above");
            MoneyCell(row.Cells[4], document.OtherAdjustments);
        }
        if (document.XeroCosts.Count == 0 && document.LabourCost == 0m && document.OtherAdjustments == 0m)
            AddEmptyRow(table, 5, "No non-work-order spend is allocated to this centre.");

        AddTotalRow(table, 4, "Xero & other costs", document.NonWoCost);
        SpaceAfterTable(section);
    }

    private static void AddSummary(Section section, CostCentreReconciliationDocument document)
    {
        SectionHeading(section, "Reconciliation");

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(11.3));
        var value = table.AddColumn(Unit.FromCentimeter(6.5));
        value.Format.Alignment = ParagraphAlignment.Right;

        void SummaryRow(string label, string amountText, bool strong = false, bool negative = false)
        {
            var row = table.AddRow();
            if (strong) row.Shading.Color = Panel;
            row.TopPadding = Unit.FromMillimeter(1.2);
            row.BottomPadding = Unit.FromMillimeter(1.2);
            var p = row.Cells[0].AddParagraph(label);
            p.Format.LeftIndent = Unit.FromMillimeter(1.5);
            p.Format.Font.Size = strong ? 9 : 8.5;
            p.Format.Font.Bold = strong;
            p.Format.Font.Color = strong ? Navy : Muted;
            var v = row.Cells[1].AddParagraph(amountText);
            v.Format.RightIndent = Unit.FromMillimeter(1.5);
            v.Format.Font.Size = strong ? 9 : 8.5;
            v.Format.Font.Bold = strong;
            v.Format.Font.Color = negative ? Negative : strong ? Navy : Ink;
        }

        SummaryRow("Sales value", Money(document.SalesValue));
        SummaryRow("Costs — work orders committed", Money(document.WoCommitted));
        SummaryRow("Costs — Xero & other", Money(document.NonWoCost));
        SummaryRow("Total forecast cost of sales", Money(document.TotalCosts));
        SummaryRow("Gross profit", Money(document.GrossProfit), strong: true, negative: document.GrossProfit < 0m);
        SummaryRow("Target cost (sales less assumed markup)", Money(document.TargetCost));
        SummaryRow("Procurement gain / loss (target less costs)", Money(document.ProcurementGainLoss),
            negative: document.ProcurementGainLoss < 0m);
        SummaryRow("Margin",
            document.MarginPercent is { } margin ? Pct(margin) : "—",
            strong: true, negative: document.GrossProfit < 0m);

        SpaceAfterTable(section);
    }

    private static void AddClosingNote(Section section, CostCentreReconciliationDocument document)
    {
        var note = section.AddParagraph(
            "All figures are net of VAT. Work-order costs are committed values (draft orders are included "
            + "and marked; rejected drafts are not). Xero figures reflect the ledger as last synced into "
            + "JPMS. Figures are gross of reconciliation-package netting — this is the centre's full "
            + $"position. Generated on {Date(document.GeneratedAt)} from the live JPMS figures.");
        note.Format.Font.Size = 8;
        note.Format.Font.Color = Muted;
        SpaceBefore(note, 2);
    }

    private static void AddFooter(Section section, CostCentreReconciliationDocument document)
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
            $"Generated {DateTime(document.GeneratedAt)} · live figures from the JPMS Financials tab",
            new Font { Color = Muted, Size = 7 });

        footer.Format.TabStops.AddTabStop(Unit.FromCentimeter(18.3), TabAlignment.Right);
    }

    // ---- Helpers ------------------------------------------------------------------------------

    private static void AddLineRow(Table table, string reference, string description, decimal amount, bool muted = false)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1);
        row.BottomPadding = Unit.FromMillimeter(1);
        TextCell(row.Cells[0], reference, mutedMono: true);
        TextCell(row.Cells[1], description, mutedMono: muted);
        MoneyCell(row.Cells[2], amount);
    }

    private static void AddEmptyRow(Table table, int columns, string message)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        row.Cells[0].MergeRight = columns - 1;
        var p = row.Cells[0].AddParagraph(message);
        p.Format.LeftIndent = Unit.FromMillimeter(1.5);
        p.Format.Font.Size = 8;
        p.Format.Font.Italic = true;
        p.Format.Font.Color = Muted;
    }

    private static void AddTotalRow(Table table, int labelSpan, string label, decimal amount)
    {
        var row = table.AddRow();
        row.Shading.Color = Panel;
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        row.Cells[0].MergeRight = labelSpan - 1;
        var p = row.Cells[0].AddParagraph(label);
        p.Format.LeftIndent = Unit.FromMillimeter(1.5);
        p.Format.Font.Size = 8.5;
        p.Format.Font.Bold = true;
        p.Format.Font.Color = Navy;
        MoneyCell(row.Cells[labelSpan], amount, bold: true);
    }

    private static void TextCell(Cell cell, string text, bool mutedMono = false)
    {
        cell.Format.LeftIndent = Unit.FromMillimeter(1.5);
        cell.Format.RightIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph(string.IsNullOrWhiteSpace(text) ? "" : text);
        p.Format.Font.Size = mutedMono ? 8 : 8.5;
        p.Format.Font.Color = mutedMono ? Muted : Ink;
    }

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
        cell.Format.RightIndent = Unit.FromMillimeter(1.5);
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


    private static string Money(decimal value) => value.ToString("£#,##0.00;-£#,##0.00", Uk);
    private static string Pct(decimal value) => value.ToString("0.#", Uk) + "%";
    private static string DateTime(DateTimeOffset value) => value.ToString("dd MMM yyyy HH:mm", Uk);

}
