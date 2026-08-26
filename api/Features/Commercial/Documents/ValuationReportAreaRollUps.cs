using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

/// <summary>
/// The printed statement shows the bill at the level the client reads it: one row per area
/// grouping — "Electrics", "Plumbing &amp; Heating" — carrying the summed money of the lines
/// priced under it and their weighted % complete, never the individual items. Areas are the
/// same consecutive runs the screen and the workbook title their lines by
/// (<see cref="ValuationReportAreas"/>); the itemised detail stays on screen and in the
/// spreadsheet for anyone who needs to trace a figure.
/// </summary>
internal static class ValuationReportAreaRollUps
{
    private const string UntitledAreaTitle = "General";

    public static IReadOnlyList<ValuationReportBillRow> For(
        IEnumerable<ValuationReportSnapshotLine> linesOfOneType, Func<string, string?> costCentreNameFor)
    {
        var areas = new List<List<ValuationReportSnapshotLine>>();
        var currentArea = "";
        foreach (var line in linesOfOneType.OrderBy(line => line.DisplayOrder))
        {
            var title = ValuationReportAreas.TitleFor(line.SectionName, line.CostCode, costCentreNameFor);
            if (areas.Count == 0 || ValuationReportAreas.StartsNewArea(title, currentArea))
            {
                currentArea = title;
                areas.Add(new List<ValuationReportSnapshotLine>());
            }
            areas[^1].Add(line);
        }
        return areas
            .Select(lines => Consolidated(lines, costCentreNameFor))
            .ToList();
    }

    private static ValuationReportBillRow Consolidated(
        IReadOnlyList<ValuationReportSnapshotLine> lines, Func<string, string?> costCentreNameFor)
    {
        var first = lines[0];
        var counting = lines.Where(line => line.CountsTowardTotals).ToList();
        var amount = counting.Sum(line => line.LineAmount);
        var claimed = counting.Sum(line => line.CumulativeClaimed);
        var period = counting.Sum(line => line.PeriodIncrement);
        var title = ValuationReportAreas.TitleFor(first.SectionName, first.CostCode, costCentreNameFor);
        return new ValuationReportBillRow(
            SharedValue(lines, line => line.CostCode),
            SharedValue(lines, line => line.ClientReference),
            string.IsNullOrWhiteSpace(title) ? UntitledAreaTitle : title,
            ItemCount(lines.Count),
            counting.Count == 0 ? "Not priced" : "",
            Quantity: null, Rate: null, amount,
            VariationRollUps.WeightedPercent(claimed, amount),
            claimed - period, period, claimed,
            CountsTowardTotals: counting.Count > 0);
    }

    // A code or client reference prints on the area row only when every line under it agrees;
    // a mixed area shows none rather than one line's value posing as the group's.
    private static string SharedValue(
        IEnumerable<ValuationReportSnapshotLine> lines, Func<ValuationReportSnapshotLine, string> valueOf)
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
