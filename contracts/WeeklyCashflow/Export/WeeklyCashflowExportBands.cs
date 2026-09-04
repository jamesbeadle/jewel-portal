using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.WeeklyCashflow.Export;

/// <summary>
/// How the grid folds into export lines. Supplier bills read one line per supplier — the plan's
/// supplier groups first, in their own order and under their own names, then every other
/// supplier A–Z. Client invoices read one line per client. A manual band reads one line per
/// item, so a recurring item's occurrences sit along one row across the weeks.
/// </summary>
public static class WeeklyCashflowExportBands
{
    public const string ClientInvoicesLabel = "Client invoices outstanding";
    public const string SupplierBillsLabel = "Supplier bills";

    /// <summary>The bands in grid order: the two Xero bands always, then each manual band that
    /// has an entry — exactly the bands the page renders.</summary>
    public static IReadOnlyList<WeeklyCashflowExportBand> For(
        WeeklyCashflowView view, IReadOnlyList<WeeklyCashflowSupplierGroup> supplierGroups)
    {
        var bands = new List<WeeklyCashflowExportBand>
        {
            new(WeeklyCashflowBand.ClientReceipts, ClientInvoicesLabel, LinesBy(In(view, WeeklyCashflowBand.ClientReceipts), entry => entry.Label)),
            new(WeeklyCashflowBand.SupplierBills, SupplierBillsLabel, SupplierLines(view, supplierGroups))
        };
        foreach (var category in WeeklyCashflowCategories.All)
        {
            var band = WeeklyCashflowMaths.BandFor(category);
            var entries = In(view, band);
            if (entries.Count == 0) continue;
            // Occurrences of one recurring item share its id; a one-off's id is equally its own.
            bands.Add(new WeeklyCashflowExportBand(band, WeeklyCashflowCategories.BandLabel(category), LinesBy(entries, entry => entry.ItemId ?? entry.Label)));
        }
        return bands;
    }

    private static List<WeeklyCashflowEntry> In(WeeklyCashflowView view, WeeklyCashflowBand band) =>
        view.Entries
            .Where(entry => entry.Band == band)
            .ToList();

    private static IReadOnlyList<WeeklyCashflowExportLine> SupplierLines(
        WeeklyCashflowView view, IReadOnlyList<WeeklyCashflowSupplierGroup> supplierGroups)
    {
        var slices = GroupSlice.For(view, supplierGroups);
        var groupedKeys = slices
            .SelectMany(slice => slice.Entries)
            .Select(entry => entry.PlacementKey)
            .ToHashSet(StringComparer.Ordinal);
        var lines = slices
            .Select(slice => new WeeklyCashflowExportLine(slice.Group.Name, slice.Entries))
            .ToList();
        var ungrouped = In(view, WeeklyCashflowBand.SupplierBills)
            .Where(entry => !groupedKeys.Contains(entry.PlacementKey));
        lines.AddRange(LinesBy(ungrouped, entry => entry.Label));
        return lines;
    }

    // One line per key, A–Z by the line's name; the first entry's label names the line.
    private static List<WeeklyCashflowExportLine> LinesBy(
        IEnumerable<WeeklyCashflowEntry> entries, Func<WeeklyCashflowEntry, string> lineKey) =>
        entries
            .GroupBy(lineKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => new WeeklyCashflowExportLine(group.First().Label, group.InGridOrder().ToList()))
            .OrderBy(line => line.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
