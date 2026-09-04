namespace Jewel.JPMS.Contracts.WeeklyCashflow;

/// <summary>The one reading order for a run of entries — the grid's, and therefore the export's:
/// soonest week first, then A–Z by name, then the placement key so equal names hold still.</summary>
public static class WeeklyCashflowEntryOrdering
{
    public static IOrderedEnumerable<WeeklyCashflowEntry> InGridOrder(this IEnumerable<WeeklyCashflowEntry> entries) =>
        entries
            .OrderBy(entry => entry.WeekIndex)
            .ThenBy(entry => entry.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.PlacementKey, StringComparer.Ordinal);
}
