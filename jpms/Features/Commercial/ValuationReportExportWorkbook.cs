using Jewel.JPMS.Services.Excel;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>Identity strip for an exported valuation report workbook.</summary>
/// <param name="StatementLabel">e.g. "VI-0004 raise", or "June 2026 — working copy" for a live export.</param>
/// <param name="PreparedLabel">e.g. "Snapshot taken 04 Aug 2026 11:39", or "Prepared 13 Aug 2026 14:02".</param>
/// <param name="IsDraft">True for live (non-snapshot) exports — stamps a working-copy warning under the band.</param>
public sealed record ValuationExportMeta(string StatementLabel, string PreparedLabel, bool IsDraft);

/// <summary>
/// One row of an exported valuation report, source-agnostic: the snapshot viewer maps frozen
/// snapshot lines here, the live valuation page maps line items + claim entries. Money is
/// cumulative-claimed maths throughout: ThisPeriod = CumulativeClaimed − PreviousClaimed.
/// </summary>
public sealed record ValuationExportLine(
    string Section,
    string Code,
    string Title,
    string LineTypeLabel,
    bool CountsTowardTotals,
    string Unit,
    decimal Quantity,
    decimal Rate,
    decimal LineAmount,
    decimal PercentComplete,
    decimal PreviousClaimed,
    decimal ThisPeriod,
    decimal CumulativeClaimed,
    string Comments)
{
    public bool MovedThisPeriod => CountsTowardTotals && ThisPeriod != 0m;
}

/// <summary>One row of the summary footer block.</summary>
public sealed record ValuationExportSummaryRow(string Label, decimal Amount, bool Strong = false);

/// <summary>
/// Builds the two-tab valuation report workbook every export shares: tab 1 is the branded
/// presentation (navy title band, sectioned bill with Previous / This period / Claimed columns,
/// gold-shaded moved lines, summary footer), tab 2 is the same lines as one flat, filterable
/// table for pivoting and reconciliation. Snapshot and live (draft) exports differ only in the
/// meta strip and the rows they map in, so the accountant always opens the same shape of file.
/// </summary>
public static class ValuationReportExportWorkbook
{
    // ---- presentation styles (deduplicated by the writer) ----
    private static readonly ExcelCellStyle Band = new(Fill: ExcelFill.Navy);
    private static readonly ExcelCellStyle BandTitle = new(Font: ExcelFont.Title, Fill: ExcelFill.Navy);
    private static readonly ExcelCellStyle BandGold = new(Font: ExcelFont.Gold, Fill: ExcelFill.Navy);
    private static readonly ExcelCellStyle BandGoldRight = new(Font: ExcelFont.Gold, Fill: ExcelFill.Navy, Align: ExcelAlign.Right);
    private static readonly ExcelCellStyle BandTextRight = new(Font: ExcelFont.BandText, Fill: ExcelFill.Navy, Align: ExcelAlign.Right);
    private static readonly ExcelCellStyle DraftWarning = new(Font: ExcelFont.Negative);
    private static readonly ExcelCellStyle Legend = new(Font: ExcelFont.SmallMuted);
    private static readonly ExcelCellStyle SectionHead = new(Font: ExcelFont.NavyBold, Border: ExcelBorder.Accent);
    private static readonly ExcelCellStyle SectionHeadFill = new(Border: ExcelBorder.Accent);
    private static readonly ExcelCellStyle ColHead = new(Font: ExcelFont.Muted, Fill: ExcelFill.Panel, Border: ExcelBorder.Hairline);
    private static readonly ExcelCellStyle ColHeadRight = ColHead with { Align = ExcelAlign.Right };

    private static ExcelCellStyle Text(bool moved) => new(Border: ExcelBorder.Hairline, Fill: FillFor(moved));
    private static ExcelCellStyle Desc(bool moved) => new(Border: ExcelBorder.Hairline, Fill: FillFor(moved), WrapText: true);
    private static ExcelCellStyle Code(bool moved) => new(Font: ExcelFont.Muted, Border: ExcelBorder.Hairline, Fill: FillFor(moved));
    private static ExcelCellStyle Num(bool moved) => new(Format: ExcelFormat.Number, Border: ExcelBorder.Hairline, Fill: FillFor(moved));
    private static ExcelCellStyle Pct(bool moved) => new(Format: ExcelFormat.Percent, Border: ExcelBorder.Hairline, Fill: FillFor(moved));
    private static ExcelCellStyle Money(bool moved, bool negative = false, bool strong = false) => new(
        Format: ExcelFormat.Currency,
        Font: negative ? ExcelFont.Negative : strong ? ExcelFont.NavyBold : ExcelFont.Default,
        Border: ExcelBorder.Hairline,
        Fill: FillFor(moved));

    private static readonly ExcelCellStyle TotalLabel = new(Font: ExcelFont.NavyBold, Fill: ExcelFill.Panel);
    private static readonly ExcelCellStyle TotalFill = new(Fill: ExcelFill.Panel);
    private static readonly ExcelCellStyle TotalMoney = new(Format: ExcelFormat.Currency, Font: ExcelFont.NavyBold, Fill: ExcelFill.Panel);
    private static readonly ExcelCellStyle SummaryLabel = new(Font: ExcelFont.Muted);
    private static readonly ExcelCellStyle SummaryMoney = new(Format: ExcelFormat.Currency);
    private static readonly ExcelCellStyle SummaryLabelStrong = new(Font: ExcelFont.NavyBold, Fill: ExcelFill.Panel);
    private static readonly ExcelCellStyle SummaryFillStrong = new(Fill: ExcelFill.Panel);
    private static readonly ExcelCellStyle SummaryMoneyStrong = new(Format: ExcelFormat.Currency, Font: ExcelFont.NavyBold, Fill: ExcelFill.Panel);

    private static ExcelFill FillFor(bool moved) => moved ? ExcelFill.Highlight : ExcelFill.None;

    public static ExcelWorkbook Build(
        ValuationExportMeta meta,
        IReadOnlyList<ValuationExportLine> lines,
        IReadOnlyList<ValuationExportSummaryRow> summary)
    {
        var workbook = new ExcelWorkbook();
        AddPresentationSheet(workbook, meta, lines, summary);
        AddRawDataSheet(workbook, lines);
        return workbook;
    }

    // ---- tab 1: the branded statement ------------------------------------

    private static void AddPresentationSheet(
        ExcelWorkbook workbook,
        ValuationExportMeta meta,
        IReadOnlyList<ValuationExportLine> lines,
        IReadOnlyList<ValuationExportSummaryRow> summary)
    {
        var sheet = workbook.AddSheet("Valuation report",
            new ExcelColumn("Code", Width: 13),
            new ExcelColumn("Description", Width: 52),
            new ExcelColumn("Unit", Width: 7),
            new ExcelColumn("Qty", Width: 9),
            new ExcelColumn("Rate", Width: 11),
            new ExcelColumn("Amount", Width: 14),
            new ExcelColumn("% Complete", Width: 11),
            new ExcelColumn("Previous", Width: 14),
            new ExcelColumn("This period", Width: 14),
            new ExcelColumn("Claimed", Width: 14));
        sheet.ShowHeaderRow = false;
        sheet.AutoFilter = false;
        sheet.FreezeHeaderRow = false;
        sheet.ShowGridLines = false;
        sheet.PrintLandscapeFitToWidth = true;

        // Title band: two navy rows spanning the full width.
        sheet.AddRow(
            new ExcelStyledCell("VALUATION REPORT", BandTitle),
            new ExcelStyledCell(null, Band), new ExcelStyledCell(null, Band), new ExcelStyledCell(null, Band),
            new ExcelStyledCell(null, Band), new ExcelStyledCell(null, Band),
            new ExcelStyledCell(meta.StatementLabel.ToUpperInvariant(), BandGoldRight),
            new ExcelStyledCell(null, Band), new ExcelStyledCell(null, Band), new ExcelStyledCell(null, Band));
        sheet.AddRow(
            new ExcelStyledCell("Jewel Bespoke Build", BandGold),
            new ExcelStyledCell(null, Band), new ExcelStyledCell(null, Band), new ExcelStyledCell(null, Band),
            new ExcelStyledCell(null, Band), new ExcelStyledCell(null, Band),
            new ExcelStyledCell(meta.PreparedLabel, BandTextRight),
            new ExcelStyledCell(null, Band), new ExcelStyledCell(null, Band), new ExcelStyledCell(null, Band));
        sheet.MergedRanges.Add("A1:F1");
        sheet.MergedRanges.Add("G1:J1");
        sheet.MergedRanges.Add("A2:F2");
        sheet.MergedRanges.Add("G2:J2");

        if (meta.IsDraft)
        {
            sheet.AddRow(new ExcelStyledCell(
                "WORKING COPY — the live report as it stands right now; figures may change until the claim is locked.",
                DraftWarning));
            sheet.MergedRanges.Add($"A{sheet.Rows.Count}:J{sheet.Rows.Count}");
        }

        sheet.AddRow(new ExcelStyledCell(
            "Shaded lines moved this period · “This period” is the movement since the previous statement · All figures net of VAT.",
            Legend));
        sheet.MergedRanges.Add($"A{sheet.Rows.Count}:J{sheet.Rows.Count}");
        sheet.AddRow(); // spacer

        // The bill, one block per section, in the order the lines arrive.
        foreach (var section in lines.GroupBy(line => line.Section))
        {
            sheet.AddRow(
                new ExcelStyledCell(section.Key, SectionHead),
                new ExcelStyledCell(null, SectionHeadFill), new ExcelStyledCell(null, SectionHeadFill),
                new ExcelStyledCell(null, SectionHeadFill), new ExcelStyledCell(null, SectionHeadFill),
                new ExcelStyledCell(null, SectionHeadFill), new ExcelStyledCell(null, SectionHeadFill),
                new ExcelStyledCell(null, SectionHeadFill), new ExcelStyledCell(null, SectionHeadFill),
                new ExcelStyledCell(null, SectionHeadFill));

            sheet.AddRow(
                new ExcelStyledCell("Code", ColHead),
                new ExcelStyledCell("Description", ColHead),
                new ExcelStyledCell("Unit", ColHead),
                new ExcelStyledCell("Qty", ColHeadRight),
                new ExcelStyledCell("Rate £", ColHeadRight),
                new ExcelStyledCell("Amount £", ColHeadRight),
                new ExcelStyledCell("% Complete", ColHeadRight),
                new ExcelStyledCell("Previous £", ColHeadRight),
                new ExcelStyledCell("This period £", ColHeadRight),
                new ExcelStyledCell("Claimed £", ColHeadRight));

            foreach (var line in section)
            {
                var moved = line.MovedThisPeriod;
                var description = line.Title;
                if (!string.IsNullOrWhiteSpace(line.Comments)) description += "\n" + line.Comments;
                if (!line.CountsTowardTotals) description += $"\n[{line.LineTypeLabel} — not priced into totals]";

                sheet.AddRow(
                    new ExcelStyledCell(line.Code, Code(moved)),
                    new ExcelStyledCell(description, Desc(moved)),
                    new ExcelStyledCell(line.Unit, Text(moved)),
                    new ExcelStyledCell(line.Quantity, Num(moved)),
                    new ExcelStyledCell(line.Rate, Num(moved)),
                    new ExcelStyledCell(line.LineAmount, Money(moved, negative: line.LineAmount < 0m)),
                    line.CountsTowardTotals
                        ? new ExcelStyledCell(line.PercentComplete / 100m, Pct(moved))
                        : new ExcelStyledCell(null, Text(moved)),
                    line.CountsTowardTotals
                        ? new ExcelStyledCell(line.PreviousClaimed, Money(moved))
                        : new ExcelStyledCell(null, Text(moved)),
                    line.CountsTowardTotals
                        ? new ExcelStyledCell(line.ThisPeriod, Money(moved, negative: line.ThisPeriod < 0m, strong: moved))
                        : new ExcelStyledCell(null, Text(moved)),
                    line.CountsTowardTotals
                        ? new ExcelStyledCell(line.CumulativeClaimed, Money(moved))
                        : new ExcelStyledCell(null, Text(moved)));
            }

            var counting = section.Where(line => line.CountsTowardTotals).ToList();
            sheet.AddRow(
                new ExcelStyledCell(null, TotalFill),
                new ExcelStyledCell($"{section.Key} total", TotalLabel),
                new ExcelStyledCell(null, TotalFill),
                new ExcelStyledCell(null, TotalFill),
                new ExcelStyledCell(null, TotalFill),
                new ExcelStyledCell(counting.Sum(line => line.LineAmount), TotalMoney),
                new ExcelStyledCell(null, TotalFill),
                new ExcelStyledCell(counting.Sum(line => line.PreviousClaimed), TotalMoney),
                new ExcelStyledCell(counting.Sum(line => line.ThisPeriod), TotalMoney),
                new ExcelStyledCell(counting.Sum(line => line.CumulativeClaimed), TotalMoney));
            sheet.AddRow(); // spacer between sections
        }

        // Summary footer — labels wide, values in the rightmost money column, as on the PDF.
        sheet.AddRow(
            new ExcelStyledCell("Valuation summary", SectionHead),
            new ExcelStyledCell(null, SectionHeadFill), new ExcelStyledCell(null, SectionHeadFill),
            new ExcelStyledCell(null, SectionHeadFill), new ExcelStyledCell(null, SectionHeadFill),
            new ExcelStyledCell(null, SectionHeadFill), new ExcelStyledCell(null, SectionHeadFill),
            new ExcelStyledCell(null, SectionHeadFill), new ExcelStyledCell(null, SectionHeadFill),
            new ExcelStyledCell(null, SectionHeadFill));
        foreach (var row in summary)
        {
            var label = row.Strong ? SummaryLabelStrong : SummaryLabel;
            var fill = row.Strong ? SummaryFillStrong : new ExcelCellStyle();
            sheet.AddRow(
                new ExcelStyledCell(null, fill),
                new ExcelStyledCell(row.Label, label),
                new ExcelStyledCell(null, fill), new ExcelStyledCell(null, fill),
                new ExcelStyledCell(null, fill), new ExcelStyledCell(null, fill),
                new ExcelStyledCell(null, fill), new ExcelStyledCell(null, fill),
                new ExcelStyledCell(null, fill),
                new ExcelStyledCell(row.Amount, row.Strong
                    ? SummaryMoneyStrong
                    : row.Amount < 0m ? SummaryMoney with { Font = ExcelFont.Negative } : SummaryMoney));
        }
    }

    // ---- tab 2: the same lines as one flat, filterable table --------------

    private static void AddRawDataSheet(ExcelWorkbook workbook, IReadOnlyList<ValuationExportLine> lines)
    {
        var sheet = workbook.AddSheet("Raw data",
            new ExcelColumn("Section"),
            new ExcelColumn("Code"),
            new ExcelColumn("Description", Width: 44),
            new ExcelColumn("Line type"),
            new ExcelColumn("Unit"),
            new ExcelColumn("Qty", ExcelFormat.Number),
            new ExcelColumn("Rate £", ExcelFormat.Currency),
            new ExcelColumn("Amount £", ExcelFormat.Currency),
            new ExcelColumn("% Complete", ExcelFormat.Percent),
            new ExcelColumn("Previous claimed £", ExcelFormat.Currency),
            new ExcelColumn("This period £", ExcelFormat.Currency),
            new ExcelColumn("Claimed £", ExcelFormat.Currency),
            new ExcelColumn("Moved this period"),
            new ExcelColumn("Counts toward totals"),
            new ExcelColumn("Comments", Width: 36));

        foreach (var line in lines)
        {
            sheet.AddRow(
                line.Section,
                line.Code,
                line.Title,
                line.LineTypeLabel,
                line.Unit,
                line.Quantity,
                line.Rate,
                line.LineAmount,
                line.CountsTowardTotals ? (decimal?)(line.PercentComplete / 100m) : null,
                line.CountsTowardTotals ? (decimal?)line.PreviousClaimed : null,
                line.CountsTowardTotals ? (decimal?)line.ThisPeriod : null,
                line.CountsTowardTotals ? (decimal?)line.CumulativeClaimed : null,
                line.MovedThisPeriod,
                line.CountsTowardTotals,
                line.Comments);
        }
    }
}
