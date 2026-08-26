using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services.Excel;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>Identity strip for an exported valuation report workbook.</summary>
/// <param name="StatementLabel">e.g. "VI-0004 raise", or "June 2026 — working copy" for a live export.</param>
/// <param name="PreparedLabel">e.g. "Snapshot taken 04 Aug 2026 11:39", or "Prepared 13 Aug 2026 14:02".</param>
/// <param name="IsDraft">True for live (non-snapshot) exports — stamps a working-copy warning under the band.</param>
public sealed record ValuationExportMeta(string StatementLabel, string PreparedLabel, bool IsDraft);

/// <summary>
/// One line of an exported valuation report, source-agnostic: the snapshot viewer maps frozen
/// snapshot lines here, the live valuation page maps line items + claim entries. Every line is
/// supplied — the workbook itself consolidates them for the Summary tab. Money is
/// cumulative-claimed maths throughout: ThisPeriod = CumulativeClaimed − PreviousClaimed.
/// </summary>
public sealed record ValuationExportLine(
    string Section,
    ValuationElementType ElementType,
    string Area,               // sub-heading within an area-grouped section (ValuationReportAreas rule); "" = untitled, continues the run above
    string Code,
    string Title,
    string LineTypeLabel,
    bool CountsTowardTotals,
    string Unit,
    decimal? Quantity,          // null on a consolidated row — several lines, no single qty/rate
    decimal? Rate,
    decimal LineAmount,
    decimal PercentComplete,
    decimal PreviousClaimed,
    decimal ThisPeriod,
    decimal CumulativeClaimed,
    string Comments,
    string VariationRef,        // variation lines only; "" otherwise
    string VariationTitle,
    string CostCode,
    int DisplayOrder) : IVariationBillLine
{
    public bool MovedThisPeriod => CountsTowardTotals && ThisPeriod != 0m;
    public bool IsVariation => ElementType == ValuationElementType.Variation;
}

/// <summary>One row of the summary footer block.</summary>
public sealed record ValuationExportSummaryRow(string Label, decimal Amount, bool Strong = false);

/// <summary>
/// Builds the three-tab valuation report workbook every export shares. "Summary" is the
/// statement as the client reads it on the PDF — one row per area of the works, one per
/// variation order (<see cref="ValuationExportRollUps"/>). "Detail" is the same branded
/// statement with every line shown under its area or its variation order. "Raw data" is every
/// line as one flat, filterable table for pivoting and reconciliation. Snapshot and live
/// (draft) exports differ only in the meta strip and the lines they map in, so the accountant
/// always opens the same shape of file.
/// </summary>
public static class ValuationReportExportWorkbook
{
    private const string SummaryLegend =
        "One row per area of the works and per variation order, as on the issued statement · every line behind these rows is on the Detail tab · "
        + "Shaded rows moved this period · “This period” is the movement since the previous statement · All figures net of VAT.";

    private const string DetailLegend =
        "Every line, under its area of the works or its variation order · "
        + "Shaded lines moved this period · “This period” is the movement since the previous statement · All figures net of VAT.";

    public static ExcelWorkbook Build(
        ValuationExportMeta meta,
        IReadOnlyList<ValuationExportLine> lines,
        IReadOnlyList<ValuationExportSummaryRow> summary)
    {
        var workbook = new ExcelWorkbook();
        var ordered = InStatementOrder(lines);
        ValuationExportStatementSheet.Add(workbook,
            new ValuationExportStatementLayout("Summary", SummaryLegend, BandTitle: _ => ""),
            meta, ValuationExportRollUps.Summarise(ordered), summary);
        ValuationExportStatementSheet.Add(workbook,
            new ValuationExportStatementLayout("Detail", DetailLegend, DetailBandTitleFor),
            meta, ordered, summary);
        ValuationExportRawDataSheet.Add(workbook, ordered);
        return workbook;
    }

    // Sections stay in the order they arrive; within the Variations section the lines read
    // order by order (V9 before V18) as they do on screen, so every line of an order sits
    // together under its band whatever the estimate's own line order was.
    private static IReadOnlyList<ValuationExportLine> InStatementOrder(IReadOnlyList<ValuationExportLine> lines) =>
        lines
            .GroupBy(line => line.Section)
            .SelectMany(section => section.First().IsVariation ? InVariationOrder(section) : section)
            .ToList();

    private static IEnumerable<ValuationExportLine> InVariationOrder(IEnumerable<ValuationExportLine> variationLines) =>
        VariationOrderRollUps.Build(variationLines)
            .SelectMany(rollUp => rollUp.Lines);

    // On the Detail tab a contract/PC/contingency line sits under its area band, a variation
    // line under its order's band ("V18 — Extra sockets to kitchen"). A blank title continues
    // the band above, the same consecutive-run rule as every other surface.
    private static string DetailBandTitleFor(ValuationExportLine line)
    {
        if (!line.IsVariation) return line.Area;
        var parts = new[] { line.VariationRef, line.VariationTitle }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part.Trim());
        return string.Join(" — ", parts);
    }
}
