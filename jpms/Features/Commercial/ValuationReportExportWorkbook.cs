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
    decimal? Quantity,
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
    int DisplayOrder,
    // The client's schedule-of-works reference for the line — same line-level-beats-map rule as
    // the PDF. Trailing default keeps the positional constructor stable for older callers.
    string ClientReference = "") : IVariationBillLine
{
    public bool MovedThisPeriod => CountsTowardTotals && ThisPeriod != 0m;
    public bool IsVariation => ElementType == ValuationElementType.Variation;
}

/// <summary>One row of the summary footer block.</summary>
public sealed record ValuationExportSummaryRow(string Label, decimal Amount, bool Strong = false);

/// <summary>
/// Builds the valuation report workbook every export shares. "Summary" is the branded statement
/// with every contract, PC and contingency line under its area, and the variations as one row
/// per variation order, as on the client's PDF (<see cref="ValuationExportRollUps"/>). Each
/// variation order then has its OWN tab carrying its lines — the detail behind the Summary's
/// consolidated row, ready for the day specific percentages are claimed on individual lines
/// (<see cref="ValuationExportVariationSheets"/>). "Pending variations" lists the orders still
/// awaiting a decision, their staged build-up and why each is pending
/// (<see cref="ValuationExportPendingSheet"/>). (The old Detail and Raw data tabs are gone —
/// accountant's request 2026-08-26: the per-order tabs ARE the detail.) Snapshot and live
/// (draft) exports differ only in the meta strip and the lines they map in, so the accountant
/// always opens the same shape of file.
/// </summary>
public static class ValuationReportExportWorkbook
{
    private const string SummaryLegend =
        "Variations as one row per order, as on the issued statement — each order's lines are on its own tab · "
        + "Shaded lines moved this period · “This period” is the movement since the previous statement · All figures net of VAT.";

    /// <param name="pendingVariations">The register's pre-approval orders for the Pending tab —
    /// null when the register could not be read, which the tab says outright rather than
    /// passing off an empty list as "none pending".</param>
    public static ExcelWorkbook Build(
        ValuationExportMeta meta,
        IReadOnlyList<ValuationExportLine> lines,
        IReadOnlyList<ValuationExportSummaryRow> summary,
        IReadOnlyList<ValuationExportPendingVariation>? pendingVariations)
    {
        var workbook = new ExcelWorkbook();
        var ordered = InStatementOrder(lines);
        // The client-reference column appears on every statement tab or none, decided by the
        // export's lines as a whole — the same one-layout rule as the PDF.
        var hasClientReference = ordered.Any(line => !string.IsNullOrWhiteSpace(line.ClientReference));
        ValuationExportStatementSheet.Add(workbook,
            new ValuationExportStatementLayout("Summary", SummaryLegend, SummaryBandTitleFor),
            meta, ValuationExportRollUps.Summarise(ordered), summary, hasClientReference);
        ValuationExportVariationSheets.Add(workbook, meta, ordered, hasClientReference);
        ValuationExportPendingSheet.Add(workbook, meta, pendingVariations);
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

    // A contract/PC/contingency line sits under its area band; the variation rows stand alone —
    // one consolidated row per order, whose lines live on the order's own tab. A blank title
    // continues the band above, the same consecutive-run rule as every other surface.
    private static string SummaryBandTitleFor(ValuationExportLine line) => line.IsVariation ? "" : line.Area;
}
