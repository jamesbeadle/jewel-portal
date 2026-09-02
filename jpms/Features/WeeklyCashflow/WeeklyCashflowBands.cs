using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Features.WeeklyCashflow;

/// <summary>How a band of the weekly grid reads: its entries in week order, and its totals per
/// column — shared by the band section and the supplier-group slicing.</summary>
public static class WeeklyCashflowBands
{
    public static IEnumerable<WeeklyCashflowEntry> EntriesFor(WeeklyCashflowView view, WeeklyCashflowBand band) =>
        view.Entries
            .Where(entry => entry.Band == band)
            .OrderBy(entry => entry.WeekIndex)
            .ThenBy(entry => entry.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.PlacementKey, StringComparer.Ordinal);

    public static decimal[] TotalsFor(WeeklyCashflowView view, WeeklyCashflowBand band)
    {
        var totals = new decimal[view.WeekStarts.Count + 1];
        foreach (var entry in view.Entries)
            if (entry.Band == band)
                totals[entry.WeekIndex] += entry.Amount;
        return totals;
    }
}
