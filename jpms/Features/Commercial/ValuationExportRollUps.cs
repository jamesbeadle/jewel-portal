using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>
/// Consolidates the exported lines into the rows the workbook's Summary tab shows — the same
/// grouping level as the client's PDF. Contract, PC and contingency lines become one row per
/// area of the works (the consecutive-run rule of <see cref="ValuationReportAreas"/>, already
/// applied when each line's Area was set); variation lines become one row per variation order
/// (<see cref="VariationOrderRollUps"/>). A consolidated row carries the summed money of the
/// lines priced under it and their weighted % complete, and no single quantity or rate.
/// </summary>
public static class ValuationExportRollUps
{
    private const string ConsolidatedLabel = "Consolidated";
    private const string UntitledAreaTitle = "General";

    public static IReadOnlyList<ValuationExportLine> Summarise(IReadOnlyList<ValuationExportLine> lines) =>
        lines
            .GroupBy(line => line.Section)
            .SelectMany(section => section.First().IsVariation ? ByVariationOrder(section) : ByArea(section))
            .ToList();

    private static IEnumerable<ValuationExportLine> ByVariationOrder(IEnumerable<ValuationExportLine> variationLines) =>
        VariationOrderRollUps.Build(variationLines)
            .Select(rollUp => rollUp.IsRolledUp ? OrderRow(rollUp) : rollUp.Lines[0]);

    private static ValuationExportLine OrderRow(VariationRollUp<ValuationExportLine> rollUp) =>
        Consolidated(rollUp.Lines, rollUp.VariationRef, rollUp.VariationTitle) with
        {
            VariationRef = rollUp.VariationRef,
            VariationTitle = rollUp.VariationTitle,
            CostCode = rollUp.CostCode
        };

    private static IEnumerable<ValuationExportLine> ByArea(IEnumerable<ValuationExportLine> sectionLines)
    {
        var areas = new List<List<ValuationExportLine>>();
        var currentArea = "";
        foreach (var line in sectionLines)
        {
            if (areas.Count == 0 || ValuationReportAreas.StartsNewArea(line.Area, currentArea))
            {
                currentArea = line.Area;
                areas.Add(new List<ValuationExportLine>());
            }
            areas[^1].Add(line);
        }
        return areas.Select(AreaRow);
    }

    private static ValuationExportLine AreaRow(IReadOnlyList<ValuationExportLine> areaLines)
    {
        var title = areaLines.Select(line => line.Area).FirstOrDefault(area => !string.IsNullOrWhiteSpace(area)) ?? UntitledAreaTitle;
        var costCode = SharedValue(areaLines, line => line.CostCode);
        return Consolidated(areaLines, costCode, title) with { CostCode = costCode };
    }

    private static ValuationExportLine Consolidated(IReadOnlyList<ValuationExportLine> lines, string code, string title)
    {
        var first = lines[0];
        var counting = lines.Where(line => line.CountsTowardTotals).ToList();
        var amount = counting.Sum(line => line.LineAmount);
        var claimed = counting.Sum(line => line.CumulativeClaimed);
        return new ValuationExportLine(
            first.Section, first.ElementType, Area: "", code, title,
            ConsolidatedLabel,
            CountsTowardTotals: counting.Count > 0,
            Unit: "", Quantity: null, Rate: null, amount,
            VariationRollUps.WeightedPercent(claimed, amount),
            counting.Sum(line => line.PreviousClaimed),
            counting.Sum(line => line.ThisPeriod),
            claimed,
            ItemCount(lines.Count),
            VariationRef: "", VariationTitle: "", CostCode: "", first.DisplayOrder);
    }

    // A cost code prints on a consolidated row only when every line under it agrees — the same
    // rule as the PDF; a mixed group shows none rather than one line's code posing as the group's.
    private static string SharedValue(IEnumerable<ValuationExportLine> lines, Func<ValuationExportLine, string> valueOf)
    {
        var distinct = lines
            .Select(line => valueOf(line).Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return distinct.Count == 1 ? distinct[0] : "";
    }

    private static string ItemCount(int count) => count == 1 ? "1 item" : $"{count} items";
}
