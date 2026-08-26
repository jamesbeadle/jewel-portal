using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>
/// Shapes the exported lines into the rows the workbook's Summary tab shows. Contract, PC and
/// contingency lines pass through untouched — the accountant wants every priced item, under
/// its area sub-heading; only variation lines consolidate, to one row per variation order
/// (<see cref="VariationOrderRollUps"/>) as on the client's PDF. A consolidated row carries the
/// summed money of the lines priced under it and their weighted % complete, priced as one item
/// at the order's total; the lines themselves are on the order's own tab.
/// </summary>
public static class ValuationExportRollUps
{
    private const string ConsolidatedLabel = "Consolidated";
    private const string ItemUnit = "item";
    private const decimal OneItem = 1m;

    public static IReadOnlyList<ValuationExportLine> Summarise(IReadOnlyList<ValuationExportLine> lines) =>
        lines
            .GroupBy(line => line.Section)
            .SelectMany(section => section.First().IsVariation ? ByVariationOrder(section) : section)
            .ToList();

    private static IEnumerable<ValuationExportLine> ByVariationOrder(IEnumerable<ValuationExportLine> variationLines) =>
        VariationOrderRollUps.Build(variationLines)
            .Select(rollUp => rollUp.IsRolledUp ? OrderRow(rollUp) : rollUp.Lines[0]);

    private static ValuationExportLine OrderRow(VariationRollUp<ValuationExportLine> rollUp)
    {
        var first = rollUp.Lines[0];
        var counting = rollUp.CountingLines.ToList();
        var claimed = counting.Sum(line => line.CumulativeClaimed);
        return new ValuationExportLine(
            first.Section, first.ElementType, Area: "", rollUp.VariationRef, rollUp.VariationTitle,
            ConsolidatedLabel,
            CountsTowardTotals: rollUp.CountsTowardTotals,
            ItemUnit, OneItem, rollUp.Amount, rollUp.Amount,
            VariationRollUps.WeightedPercent(claimed, rollUp.Amount),
            counting.Sum(line => line.PreviousClaimed),
            counting.Sum(line => line.ThisPeriod),
            claimed,
            ItemCount(rollUp.Lines.Count),
            rollUp.VariationRef, rollUp.VariationTitle, rollUp.CostCode, first.DisplayOrder);
    }

    private static string ItemCount(int count) => count == 1 ? "1 item" : $"{count} items";
}
