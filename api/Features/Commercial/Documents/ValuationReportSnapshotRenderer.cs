using System.Globalization;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

/// <summary>
/// Everything the snapshot PDF needs beyond the frozen detail itself: the project identity for
/// the header, and whether this is a frozen snapshot or a working copy of the live report
/// (draft exports render the same statement with working-copy stamps instead of the immutable
/// -record wording). Assembled by <see cref="ValuationReportSnapshotPdfBuilder"/>.
/// </summary>
public sealed record ValuationReportSnapshotDocument(
    string ProjectReference,
    string ProjectName,
    string ClientName,
    ValuationReportSnapshotDetail Detail,
    bool IsDraft = false,
    // Cost code → master name, for the bill's area sub-headings when a line carries no
    // estimate section (ValuationReportAreas rule). Null renders codes rather than names.
    IReadOnlyDictionary<string, string>? CostCentreNames = null);

/// <summary>
/// Renders one frozen valuation-report snapshot into a branded PDF using PDFsharp/MigraDoc: the
/// same grouped-bill + summary-footer layout as the on-screen snapshot viewer (Contract Works,
/// Provisional Sums, Contingency Sums, Variations), fed entirely from the snapshot's copied lines
/// — live report edits never show here, exactly as on screen. Each line carries the movement
/// story the accountant traces a claim by: Previous / This period / Claimed, with lines that
/// moved this period shaded gold. Pure function of the document model, so the download endpoint
/// and the email attachment render identically.
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
    // Warm gold tint behind lines that moved this period — light enough to print.
    private static readonly Color Highlight = new(0xFB, 0xF2, 0xE2);

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
        pdf.Info.Subject = document.IsDraft ? "Valuation report (working copy)" : "Valuation report";

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
        AddMovementLegend(section, document);

        // One column layout for the whole statement: the client-reference column appears in
        // every bill section or none, decided by the document's lines as a whole.
        var columns = ValuationReportBillColumns.For(document.Detail.Lines);
        AddElementGroup(section, document, columns, "Contract Works", ValuationElementType.ContractWorks);
        AddElementGroup(section, document, columns, "Provisional Sums", ValuationElementType.PcSum);
        AddElementGroup(section, document, columns, "Contingency Sums", ValuationElementType.Contingency);
        AddElementGroup(section, document, columns, "Variations", ValuationElementType.Variation);

        AddSummary(section, document.Detail);
        AddClosingNote(section, document);
        AddFooter(section, document);

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

        // The official Jewel Bespoke Build logo leads the band — the gold/orange registered
        // artwork reads directly on the navy ground (embedded once in DocumentBranding).
        DocumentBranding.AddLogo(row.Cells[0], Unit.FromCentimeter(3.4), Unit.FromMillimeter(1.5));

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

        var date = row.Cells[1].AddParagraph(document.IsDraft
            ? $"Prepared  {DateTime(snapshot.TakenAt)}"
            : $"Snapshot taken  {DateTime(snapshot.TakenAt)}");
        date.Format.Font.Size = 8;
        date.Format.Font.Color = Gold;

        if (document.IsDraft)
        {
            var draft = row.Cells[1].AddParagraph("WORKING COPY — NOT AN ISSUED STATEMENT");
            draft.Format.Font.Size = 8;
            draft.Format.Font.Bold = true;
            draft.Format.Font.Color = Orange;
            SpaceBefore(draft, 1.5);
        }

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
            document.IsDraft ? "Prepared" : "Snapshot taken", DateTime(snapshot.TakenAt));
        AddGridRow(table,
            "Revised contract sum", Money(snapshot.RevisedContractSum),
            "Total works complete", Money(snapshot.TotalWorksComplete));
        AddGridRow(table,
            "Certified to date", Money(snapshot.CertifiedToDate),
            "Payment due (ex VAT)", Money(snapshot.PaymentDueExVat));

        SpaceAfterTable(section);
    }

    /// <summary>
    /// One line telling the reader how to trace the claim: what the movement columns mean and
    /// what the gold shading marks. Sits between the details grid and the first bill section.
    /// </summary>
    private static void AddMovementLegend(Section section, ValuationReportSnapshotDocument document)
    {
        var legend = section.AddParagraph();
        legend.Format.Font.Size = 7.5;
        legend.Format.Font.Color = Muted;
        legend.AddFormattedText("◆ ", new Font { Color = Orange, Size = 7.5 });
        legend.AddText("Lines shaded gold moved this period. ");
        legend.AddFormattedText("Previous", new Font { Bold = true, Size = 7.5, Color = Muted });
        legend.AddText(" is the cumulative value claimed on the statement before this one, ");
        legend.AddFormattedText("This period", new Font { Bold = true, Size = 7.5, Color = Muted });
        legend.AddText(" the movement now being claimed, ");
        legend.AddFormattedText("Claimed", new Font { Bold = true, Size = 7.5, Color = Muted });
        legend.AddText(" the cumulative total to date. All figures net of VAT.");
        SpaceAfter(legend, 1.5);
    }

    private static void AddElementGroup(
        Section section, ValuationReportSnapshotDocument document, ValuationReportBillColumns columns,
        string title, ValuationElementType elementType)
    {
        // Variations print consolidated per variation per cost centre (ValuationReportBillRows).
        var rows = ValuationReportBillRows.For(document.Detail.Lines, elementType,
            code => document.CostCentreNames is { } names && names.TryGetValue(code, out var name) ? name : null);
        if (rows.Count == 0)
            return;

        SectionHeading(section, title);
        var table = BillTable(section, columns);
        AddBillHeader(table, columns);

        var currentArea = "";
        foreach (var row in rows)
        {
            // Area sub-headings — the estimate's own section titles ("Electrics", "Plumbing &
            // Heating"), else the line's cost-centre name — so the statement reads in the same
            // titled areas as the estimate it was priced from. Same shared rule as the screen
            // and the workbook (ValuationReportAreas); consecutive runs in display order.
            if (ValuationReportAreas.StartsNewArea(row.AreaTitle, currentArea))
            {
                currentArea = row.AreaTitle;
                AddAreaRow(table, columns, row.AreaTitle);
            }
            AddBillRow(table, columns, row);
        }

        AddBillTotals(table, columns, title, rows);
        SpaceAfterTable(section);
    }

    private static Table BillTable(Section section, ValuationReportBillColumns columns)
    {
        var table = section.AddTable();
        table.Borders.Color = Hair;
        table.Borders.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(columns.CodeWidthCentimetres));
        if (columns.HasClientReference)
            table.AddColumn(Unit.FromCentimeter(columns.ClientReferenceWidthCentimetres));
        table.AddColumn(Unit.FromCentimeter(columns.DescriptionWidthCentimetres));
        var qty = table.AddColumn(Unit.FromCentimeter(columns.QuantityWidthCentimetres));
        var rate = table.AddColumn(Unit.FromCentimeter(columns.RateWidthCentimetres));
        var amount = table.AddColumn(Unit.FromCentimeter(columns.AmountWidthCentimetres));
        var percent = table.AddColumn(Unit.FromCentimeter(columns.PercentWidthCentimetres));
        var previous = table.AddColumn(Unit.FromCentimeter(columns.PreviousWidthCentimetres));
        var period = table.AddColumn(Unit.FromCentimeter(columns.PeriodWidthCentimetres));
        var claimed = table.AddColumn(Unit.FromCentimeter(columns.ClaimedWidthCentimetres));
        qty.Format.Alignment = ParagraphAlignment.Right;
        rate.Format.Alignment = ParagraphAlignment.Right;
        amount.Format.Alignment = ParagraphAlignment.Right;
        percent.Format.Alignment = ParagraphAlignment.Right;
        previous.Format.Alignment = ParagraphAlignment.Right;
        period.Format.Alignment = ParagraphAlignment.Right;
        claimed.Format.Alignment = ParagraphAlignment.Right;
        return table;
    }

    private static void AddBillHeader(Table table, ValuationReportBillColumns columns)
    {
        var header = table.AddRow();
        header.Shading.Color = Panel;
        header.TopPadding = Unit.FromMillimeter(1.2);
        header.BottomPadding = Unit.FromMillimeter(1.2);
        header.HeadingFormat = true;
        HeaderCell(header.Cells[columns.Code], "Code");
        if (columns.HasClientReference)
            HeaderCell(header.Cells[columns.ClientReference], "Client ref");
        HeaderCell(header.Cells[columns.Description], "Description");
        HeaderCell(header.Cells[columns.Quantity], "Qty");
        HeaderCell(header.Cells[columns.Rate], "Rate");
        HeaderCell(header.Cells[columns.Amount], "Amount");
        HeaderCell(header.Cells[columns.Percent], "%");
        HeaderCell(header.Cells[columns.Previous], "Previous");
        HeaderCell(header.Cells[columns.Period], "This period");
        HeaderCell(header.Cells[columns.Claimed], "Claimed");
    }

    private static void AddAreaRow(Table table, ValuationReportBillColumns columns, string area)
    {
        var areaRow = table.AddRow();
        areaRow.Shading.Color = Panel;
        areaRow.TopPadding = Unit.FromMillimeter(1.4);
        areaRow.BottomPadding = Unit.FromMillimeter(1);
        areaRow.KeepWith = 1; // never strand a title at the foot of a page
        areaRow.Cells[columns.Code].MergeRight = columns.Last;
        var areaTitle = areaRow.Cells[columns.Code].AddParagraph(area.ToUpperInvariant());
        areaTitle.Format.LeftIndent = Unit.FromMillimeter(1.5);
        areaTitle.Format.Font.Size = 7.5;
        areaTitle.Format.Font.Bold = true;
        areaTitle.Format.Font.Color = Navy;
    }

    private static void AddBillRow(Table table, ValuationReportBillColumns columns, ValuationReportBillRow line)
    {
        var moved = line.MovedThisPeriod;
        var row = table.AddRow();
        row.TopPadding = Unit.FromMillimeter(1.2);
        row.BottomPadding = Unit.FromMillimeter(1.2);
        // The gold tint is the accountant's scan line: only rows that moved carry it.
        if (moved) row.Shading.Color = Highlight;

        var code = row.Cells[columns.Code].AddParagraph(line.Code);
        code.Format.Font.Size = 8;
        code.Format.Font.Bold = true;
        code.Format.Font.Color = line.CountsTowardTotals ? Navy : Muted;

        // The client's own schedule-of-works item number — the figure they reconcile by.
        if (columns.HasClientReference)
        {
            var clientReference = row.Cells[columns.ClientReference].AddParagraph(line.ClientReference);
            clientReference.Format.Font.Size = 8;
            clientReference.Format.Font.Color = line.CountsTowardTotals ? Navy : Muted;
        }

        AddDescription(row.Cells[columns.Description], line);
        NumberCell(row.Cells[columns.Quantity], line.Quantity);
        NumberCell(row.Cells[columns.Rate], line.Rate);
        MoneyCell(row.Cells[columns.Amount], line.Amount,
            colour: line.Amount < 0 ? Negative : line.CountsTowardTotals ? null : Muted);

        var pct = row.Cells[columns.Percent].AddParagraph(line.CountsTowardTotals ? Pct(line.PercentComplete) : "—");
        pct.Format.Font.Size = 8;
        pct.Format.Font.Color = Muted;

        if (!line.CountsTowardTotals)
        {
            DashCell(row.Cells[columns.Previous]);
            DashCell(row.Cells[columns.Period]);
            DashCell(row.Cells[columns.Claimed]);
            return;
        }
        MoneyCell(row.Cells[columns.Previous], line.PreviousClaimed, colour: Muted);
        // The figure this statement exists to show — bold on the rows that moved.
        MoneyCell(row.Cells[columns.Period], line.PeriodIncrement, bold: moved,
            colour: line.PeriodIncrement < 0 ? Negative : moved ? Navy : Muted);
        MoneyCell(row.Cells[columns.Claimed], line.CumulativeClaimed);
    }

    private static void AddDescription(Cell cell, ValuationReportBillRow line)
    {
        var title = cell.AddParagraph(line.Title);
        title.Format.Font.Size = 8.5;
        title.Format.Font.Color = line.CountsTowardTotals ? Ink : Muted;
        if (!string.IsNullOrWhiteSpace(line.Comments))
        {
            var comment = cell.AddParagraph(line.Comments);
            comment.Format.Font.Size = 7.5;
            comment.Format.Font.Color = Muted;
        }
        if (string.IsNullOrWhiteSpace(line.KindLabel))
            return;
        var kind = cell.AddParagraph(line.KindLabel.ToUpperInvariant());
        kind.Format.Font.Size = 7;
        kind.Format.Font.Color = Muted;
    }

    private static void AddBillTotals(Table table, ValuationReportBillColumns columns, string title, IReadOnlyList<ValuationReportBillRow> rows)
    {
        var counting = rows.Where(row => row.CountsTowardTotals).ToList();
        var totals = table.AddRow();
        totals.Shading.Color = Panel;
        totals.TopPadding = Unit.FromMillimeter(1.4);
        totals.BottomPadding = Unit.FromMillimeter(1.4);
        var label = totals.Cells[columns.Description].AddParagraph($"{title} total");
        label.Format.Font.Size = 8.5;
        label.Format.Font.Bold = true;
        label.Format.Font.Color = Navy;
        MoneyCell(totals.Cells[columns.Amount], counting.Sum(row => row.Amount), bold: true);
        MoneyCell(totals.Cells[columns.Previous], counting.Sum(row => row.PreviousClaimed), bold: true);
        MoneyCell(totals.Cells[columns.Period], counting.Sum(row => row.PeriodIncrement), bold: true);
        MoneyCell(totals.Cells[columns.Claimed], counting.Sum(row => row.CumulativeClaimed), bold: true);
    }

    private static void AddSummary(Section section, ValuationReportSnapshotDetail detail)
    {
        var snapshot = detail.Snapshot;

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

        // The movement being claimed on this statement — the same total as the per-section
        // "This period" columns, so the bill and the summary reconcile by inspection.
        var periodTotal = detail.Lines
            .Where(line => line.CountsTowardTotals)
            .Sum(line => line.PeriodIncrement);

        SummaryRow("Original contract sum", snapshot.ContractSum);
        SummaryRow("Net variations", snapshot.NetVariations);
        SummaryRow("Revised contract sum", snapshot.RevisedContractSum, strong: true);
        SummaryRow("Total works complete", snapshot.TotalWorksComplete);
        SummaryRow("Works claimed this period", periodTotal);
        SummaryRow($"Retention held ({Pct(snapshot.RetentionPercent)})", snapshot.RetentionHeld);
        SummaryRow($"Retention released ({Pct(snapshot.RetentionReleasePercent)})", snapshot.RetentionReleased);
        SummaryRow("Certified to date", snapshot.CertifiedToDate);
        // Only projects with a cash-up-front deposit show the deduction block — everyone
        // else's summary keeps its familiar By France shape. Mirrors the workbook: total
        // payable, less the deposit released, equals the amount actually invoiced.
        if (snapshot.DepositPercent > 0m || snapshot.DepositReleased != 0m)
        {
            SummaryRow("Payment due before deposit (ex VAT)", snapshot.PaymentDueExVat + snapshot.DepositReleased);
            SummaryRow($"Less deposit released ({Pct(snapshot.DepositPercent)})", snapshot.DepositReleased);
        }
        SummaryRow("Payment due (ex VAT)", snapshot.PaymentDueExVat, strong: true);

        SpaceAfterTable(section);
    }

    private static void AddClosingNote(Section section, ValuationReportSnapshotDocument document)
    {
        var snapshot = document.Detail.Snapshot;
        var note = section.AddParagraph(document.IsDraft
            ? "All figures are net of VAT. This is a WORKING COPY of the live valuation report as it stood "
              + $"when prepared on {Date(snapshot.TakenAt)} — figures may change until the claim is locked "
              + "and a snapshot is taken; nothing here has been issued to anyone. Declined and "
              + "to-be-confirmed lines are shown for completeness but are not priced into any total."
            : "All figures are net of VAT. This statement is a frozen record of the valuation report exactly "
              + $"as it stood when the snapshot was taken on {Date(snapshot.TakenAt)}; work recorded since is "
              + "not reflected here. Declined and to-be-confirmed lines are shown for completeness but are not "
              + "priced into any total. If anything on this statement doesn't match your records, please get "
              + "in touch so we can reconcile it together.");
        note.Format.Font.Size = 8;
        note.Format.Font.Color = Muted;
        SpaceBefore(note, 2);
    }

    private static void AddFooter(Section section, ValuationReportSnapshotDocument document)
    {
        var snapshot = document.Detail.Snapshot;
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
            document.IsDraft
                ? $"Prepared {DateTime(snapshot.TakenAt)} · working copy of the live report — not an issued statement"
                : $"Snapshot taken {DateTime(snapshot.TakenAt)} · immutable record from the JPMS register",
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
        p.Format.Font.Size = 8;
        p.Format.Font.Bold = bold;
        p.Format.Font.Color = colour ?? Ink;
    }

    // Quantity / rate: a consolidated variation row has neither, and prints a dash instead.
    private static void NumberCell(Cell cell, decimal? value)
    {
        var p = cell.AddParagraph(value is { } number ? Num(number) : "—");
        p.Format.Font.Size = 8;
        p.Format.Font.Color = Muted;
    }

    private static void DashCell(Cell cell)
    {
        cell.Format.RightIndent = Unit.FromMillimeter(1.5);
        var p = cell.AddParagraph("—");
        p.Format.Font.Size = 8;
        p.Format.Font.Color = Muted;
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
