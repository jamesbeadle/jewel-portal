using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>
/// Shapes the exported lines into the rows the workbook's Summary tab shows. Contract, PC and
/// contingency lines pass through untouched — the accountant wants every priced item, under
/// its area sub-heading; only variation lines consolidate, to one row per variation order
/// (<see cref="VariationOrderRollUps"/>) as on the client's PDF. Only APPROVED orders are
/// listed — an order with nothing priced into the totals (TBC placeholders left by a return to
/// quoting, declined work) is not on the statement at all; its story is the Pending tab and the
/// register (accountant 2026-08-26). A consolidated row carries the summed money of the lines
/// priced under it and their weighted % complete, priced as one item at the order's total; the
/// lines themselves are on the order's own tab.
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
            .Where(rollUp => rollUp.CountsTowardTotals)
            .Select(rollUp => rollUp.IsRolledUp ? OrderRow(rollUp) : rollUp.Lines[0]);

    private static ValuationExportLine OrderRow(VariationRollUp<ValuationExportLine> rollUp)
    {
        var first = rollUp.Lines[0];
        var counting = rollUp.CountingLines.ToList();
        var claimed = counting.Sum(line => line.CumulativeClaimed);
        return new ValuationExportLine(
            first.Section, first.ElementType, Area: "", VariationRefs.Padded(rollUp.VariationRef), rollUp.VariationTitle,
            ConsolidatedLabel,
            CountsTowardTotals: rollUp.CountsTowardTotals,
            ItemUnit, OneItem, rollUp.Amount, rollUp.Amount,
            VariationRollUps.WeightedPercent(claimed, rollUp.Amount),
            counting.Sum(line => line.PreviousClaimed),
            counting.Sum(line => line.ThisPeriod),
            claimed,
            ItemCount(rollUp.Lines.Count),
            rollUp.VariationRef, rollUp.VariationTitle, rollUp.CostCode, first.DisplayOrder,
            SharedClientReference(rollUp.Lines));
    }

    // The client's reference prints on the order's row only when every line under it agrees;
    // a mixed order shows none rather than one line's reference posing as the order's.
    private static string SharedClientReference(IEnumerable<ValuationExportLine> lines)
    {
        var distinct = lines
            .Select(line => line.ClientReference.Trim())
            .Where(reference => reference.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return distinct.Count == 1 ? distinct[0] : "";
    }

    private static string ItemCount(int count) => count == 1 ? "1 item" : $"{count} items";
}
