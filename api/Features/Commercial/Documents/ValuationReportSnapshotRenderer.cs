using System.Globalization;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Models;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

/// <summary>
/// Everything the snapshot PDF needs beyond the frozen detail itself: the project identity for
/// the header. Assembled by <see cref="ValuationReportSnapshotPdfBuilder"/>.
/// </summary>
public sealed record ValuationReportSnapshotDocument(
    string ProjectReference,
    string ProjectName,
    string ClientName,
    ValuationReportSnapshotDetail Detail);

/// <summary>
/// Renders one frozen valuation-report snapshot into a branded PDF using PDFsharp/MigraDoc: the
/// same grouped-bill + summary-footer layout as the on-screen snapshot viewer (Contract Works,
/// Provisional Sums, Contingency Sums, Variations), fed entirely from the snapshot's copied lines
/// — live report edits never show here, exactly as on screen. Pure function of the document
/// model, so the download endpoint and the email attachment render identically.
/// Follows the JewelBB palette established by <see cref="Progress.Documents.ProgressReportRenderer"/>.
/// </summary>
public static class ValuationReportSnapshotRenderer
{
    // JewelBB palette — matches ProgressReportRenderer / SubcontractorStatementRenderer.
    private static readonly Color Navy = new(0x1A, 0x1E, 0x29);
    private static readonly Color Orange = new(0xFF, 0x83, 0x00);
    private static readonly Color Gold = new(0xC0, 0x9A, 0x51);
    private static readonly Color White = new(0xFF, 0xFF, 0xFF);
    private static readonly Color Panel = new(0xF3, 0xF3, 0xF5);
    private static readonly Color Hair = new(0xDD, 0xDD, 0xE1);
    private static readonly Color Muted = new(0x60, 0x66, 0x72);
    private static readonly Color Ink = new(0x22, 0x26, 0x30);
    private static readonly Color Negative = new(0xB4, 0x23, 0x18);

    private const string FontFamily = "JPMS Sans";
    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");

    private static readonly object FontGate = new();
    private static bool _fontsReady;

    public static byte[] Render(ValuationReportSnapshotDocument document)
    {
        EnsureFonts();

        var snapshot = document.Detail.Snapshot;

        var pdf = new Document();
        pdf.Info.Title = $"{document.ProjectName} Valuation Report — {snapshot.Label}".Trim();
        pdf.Info.Author = "Jewel Bespoke Build";
        pdf.Info.Subject = "Valuation report";

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

        AddElementGroup(section, document.Detail, "Contract Works", ValuationElementType.ContractWorks);
        AddElementGroup(section, document.Detail, "Provisional Sums", ValuationElementType.PcSum);
        AddElementGroup(section, document.Detail, "Contingency Sums", ValuationElementType.Contingency);
        AddElementGroup(section, document.Detail, "Variations", ValuationElementType.Variation);

        AddSummary(section, snapshot);
        AddClosingNote(section, snapshot);
        AddFooter(section, snapshot);

        var renderer = new PdfDocumentRenderer { Document = pdf };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    // ---- Sections -----------------------------------------------------------------------------

    private static void AddHeaderBand(Section section, ValuationReportSnapshotDocument document)
    {
        var snapshot = document.Detail.Snapshot;

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

        var heading = row.Cells[0].AddParagraph("VALUATION REPORT");
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

        var stamp = row.Cells[1].AddParagraph(snapshot.Label.ToUpperInvariant());
        stamp.Format.Font.Size = 10;
        stamp.Format.Font.Bold = true;
        stamp.Format.Font.Color = White;
        SpaceAfter(stamp, 2);

        var date = row.Cells[1].AddParagraph($"Snapshot taken  {DateTime(snapshot.TakenAt)}");
        date.Format.Font.Size = 8;
        date.Format.Font.Color = Gold;

        Hairline(section);
    }

    private static void AddDetailsGrid(Section section, ValuationReportSnapshotDocument document)
    {
        var snapshot = document.Detail.Snapshot;

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
            "Statement", snapshot.Label,
            "Snapshot taken", DateTime(snapshot.TakenAt));
        AddGridRow(table,
            "Revised contract sum", Money(snapshot.RevisedContractSum),
            "Total works complete", Money(snapshot.TotalWorksComplete));
        AddGridRow(table,
            "Certified to date", Money(snapshot.CertifiedToDate),
            "Payment due (ex VAT)", Money(snapshot.PaymentDueExVat));

        SpaceAfterTable(section);
    }

    private static void AddElementGroup(
        Section section, ValuationReportSnapshotDetail detail, string title, ValuationElementType elementType)
    {
        var lines = detail.Lines
            .Where(line => line.ElementType == elementType)
            .OrderBy(line => line.DisplayOrder)
            .ToList();
        if (lines.Count == 0)
            return;

        SectionHeading(section, title);

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(1.7));                              // code
        table.AddColumn(Unit.FromCentimeter(6.0));                              // description
        var qty = table.AddColumn(Unit.FromCentimeter(1.5));
        var rate = table.AddColumn(Unit.FromCentimeter(2.1));
        var amount = table.AddColumn(Unit.FromCentimeter(2.3));
        var percent = table.AddColumn(Unit.FromCentimeter(1.6));
        var claimed = table.AddColumn(Unit.FromCentimeter(2.6));
        qty.Format.Alignment = ParagraphAlignment.Right;
        rate.Format.Alignment = ParagraphAlignment.Right;
        amount.Format.Alignment = ParagraphAlignment.Right;
        percent.Format.Alignment = ParagraphAlignment.Right;
        claimed.Format.Alignment = ParagraphAlignment.Right;

        var header = table.AddRow();
        header.Shading.Color = Panel;
        header.TopPadding = Unit.FromMillimeter(1.2);
        header.BottomPadding = Unit.FromMillimeter(1.2);
        header.HeadingFormat = true;
        HeaderCell(header.Cells[0], "Code");
        HeaderCell(header.Cells[1], "Description");
        HeaderCell(header.Cells[2], "Qty");
        HeaderCell(header.Cells[3], "Rate");
        HeaderCell(header.Cells[4], "Amount");
        HeaderCell(header.Cells[5], "% Complete");
        HeaderCell(header.Cells[6], "Claimed");

        foreach (var line in lines)
        {
            var row = table.AddRow();
            row.TopPadding = Unit.FromMillimeter(1.2);
            row.BottomPadding = Unit.FromMillimeter(1.2);

            var code = row.Cells[0].AddParagraph(CodeFor(line));
            code.Format.Font.Size = 8;
            code.Format.Font.Bold = true;
            code.Format.Font.Color = line.CountsTowardTotals ? Navy : Muted;

            var title2 = row.Cells[1].AddParagraph(TitleFor(line));
            title2.Format.Font.Size = 8.5;
            title2.Format.Font.Color = line.CountsTowardTotals ? Ink : Muted;
            if (!string.IsNullOrWhiteSpace(line.Comments))
            {
                var comment = row.Cells[1].AddParagraph(line.Comments);
                comment.Format.Font.Size = 7.5;
                comment.Format.Font.Color = Muted;
            }
            if (!line.CountsTowardTotals)
            {
                var kind = row.Cells[1].AddParagraph(LineTypeLabel(line.LineType).ToUpperInvariant());
                kind.Format.Font.Size = 7;
                kind.Format.Font.Color = Muted;
            }

            var qtyCell = row.Cells[2].AddParagraph(Num(line.Quantity));
            qtyCell.Format.Font.Size = 8.5;
            qtyCell.Format.Font.Color = Muted;

            var rateCell = row.Cells[3].AddParagraph(Num(line.Rate));
            rateCell.Format.Font.Size = 8.5;
            rateCell.Format.Font.Color = Muted;

            MoneyCell(row.Cells[4], line.LineAmount,
                colour: line.LineAmount < 0 ? Negative : line.CountsTowardTotals ? null : Muted);

            var pct = row.Cells[5].AddParagraph(line.CountsTowardTotals ? Pct(line.PercentComplete) : "—");
            pct.Format.Font.Size = 8.5;
            pct.Format.Font.Color = Muted;

            if (line.CountsTowardTotals)
                MoneyCell(row.Cells[6], line.CumulativeClaimed);
            else
            {
                var dash = row.Cells[6].AddParagraph("—");
                dash.Format.Font.Size = 8.5;
                dash.Format.Font.Color = Muted;
            }
        }

        var counting = lines.Where(line => line.CountsTowardTotals).ToList();
        var totals = table.AddRow();
        totals.Shading.Color = Panel;
        totals.TopPadding = Unit.FromMillimeter(1.4);
        totals.BottomPadding = Unit.FromMillimeter(1.4);
        var label = totals.Cells[1].AddParagraph($"{title} total");
        label.Format.Font.Size = 8.5;
        label.Format.Font.Bold = true;
        label.Format.Font.Color = Navy;
        MoneyCell(totals.Cells[4], counting.Sum(line => line.LineAmount), bold: true);
        MoneyCell(totals.Cells[6], counting.Sum(line => line.CumulativeClaimed), bold: true);

        SpaceAfterTable(section);
    }

    private static void AddSummary(Section section, ValuationReportSnapshot snapshot)
    {
        SectionHeading(section, "Valuation summary");

        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(11.3));
        var value = table.AddColumn(Unit.FromCentimeter(6.5));
        value.Format.Alignment = ParagraphAlignment.Right;

        void SummaryRow(string label, decimal amount, bool strong = false)
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
            MoneyCell(row.Cells[1], amount, bold: strong,
                colour: amount < 0 ? Negative : null);
        }

        SummaryRow("Original contract sum", snapshot.ContractSum);
        SummaryRow("Net variations", snapshot.NetVariations);
        SummaryRow("Revised contract sum", snapshot.RevisedContractSum, strong: true);
        SummaryRow("Total works complete", snapshot.TotalWorksComplete);
        SummaryRow($"Retention held ({Pct(snapshot.RetentionPercent)})", snapshot.RetentionHeld);
        SummaryRow($"Retention released ({Pct(snapshot.RetentionReleasePercent)})", snapshot.RetentionReleased);
        SummaryRow("Certified to date", snapshot.CertifiedToDate);
        SummaryRow("Payment due (ex VAT)", snapshot.PaymentDueExVat, strong: true);

        SpaceAfterTable(section);
    }

    private static void AddClosingNote(Section section, ValuationReportSnapshot snapshot)
    {
        var note = section.AddParagraph(
            "All figures are net of VAT. This statement is a frozen record of the valuation report exactly "
            + $"as it stood when the snapshot was taken on {Date(snapshot.TakenAt)}; work recorded since is "
            + "not reflected here. Declined and to-be-confirmed lines are shown for completeness but are not "
            + "priced into any total. If anything on this statement doesn't match your records, please get "
            + "in touch so we can reconcile it together.");
        note.Format.Font.Size = 8;
        note.Format.Font.Color = Muted;
        SpaceBefore(note, 2);
    }

    private static void AddFooter(Section section, ValuationReportSnapshot snapshot)
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
            $"Snapshot taken {DateTime(snapshot.TakenAt)} · immutable record from the JPMS register",
            new Font { Color = Muted, Size = 7 });

        footer.Format.TabStops.AddTabStop(Unit.FromCentimeter(18.3), TabAlignment.Right);
    }

    // ---- Helpers ------------------------------------------------------------------------------

    // Same code/title fallbacks as the on-screen snapshot viewer, so PDF and screen always agree.
    private static string CodeFor(ValuationReportSnapshotLine line) =>
        line.ElementType == ValuationElementType.Variation
            ? (string.IsNullOrWhiteSpace(line.VariationRef) ? line.CostCode : line.VariationRef)
            : (string.IsNullOrWhiteSpace(line.CostCode) ? line.SectionCode : line.CostCode);

    private static string TitleFor(ValuationReportSnapshotLine line)
    {
        if (line.ElementType == ValuationElementType.Variation)
            return string.IsNullOrWhiteSpace(line.VariationTitle) ? line.Description : line.VariationTitle;
        if (!string.IsNullOrWhiteSpace(line.Description)) return line.Description;
        return line.SectionName;
    }

    private static string LineTypeLabel(ValuationLineType type) => type switch
    {
        ValuationLineType.ProvisionalSum => "Provisional sum",
        ValuationLineType.Omit => "Omit",
        ValuationLineType.Declined => "Declined",
        ValuationLineType.Tbc => "TBC",
        _ => type.ToString()
    };

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

    private static string Money(decimal value) => value.ToString("£#,##0.00;-£#,##0.00", Uk);
    private static string Num(decimal value) => value.ToString("0.##", Uk);
    private static string Pct(decimal value) => value.ToString("0.##", Uk) + "%";
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
