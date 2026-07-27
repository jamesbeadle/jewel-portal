namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Works out which report lines a submitted build-up re-prices, which it adds and which it drops.
///
/// The distinction is the whole reason a claimed variation can be edited at all. A re-priced line
/// keeps its ValuationLineItemId, so every claim entry already recorded against it stays attached;
/// a dropped line takes its entries with it, which is only ever safe while nothing settled has been
/// claimed against it. Pairing by POSITION cannot tell those two apart — delete the first of two
/// rows and position 1 silently becomes a different piece of work — so the pairing is by id, and a
/// row that names no id is a row the user has just added.
/// </summary>
public sealed record VariationLineRevision(
    IReadOnlyList<RepricedVariationLine> Repriced,
    IReadOnlyList<VariationLineInput> Added,
    IReadOnlyList<string> Dropped)
{
    public static VariationLineRevision Plan(
        IReadOnlyList<string> existingLineItemIds,
        IReadOnlyList<VariationLineInput> submitted)
    {
        var existing = new HashSet<string>(existingLineItemIds);
        var repriced = new List<RepricedVariationLine>();
        var added = new List<VariationLineInput>();
        var paired = new HashSet<string>();

        foreach (var line in submitted)
        {
            var id = line.ValuationLineItemId;
            // An id that isn't on the report, or one a previous row already claimed, is a new line:
            // two rows can never re-price the same report line between them.
            if (string.IsNullOrWhiteSpace(id) || !existing.Contains(id) || !paired.Add(id))
            {
                added.Add(line);
                continue;
            }
            repriced.Add(new RepricedVariationLine(id, line));
        }

        return new VariationLineRevision(
            repriced,
            added,
            existingLineItemIds.Where(id => !paired.Contains(id)).ToList());
    }
}

/// <summary>A report line and the figures it is being re-priced to.</summary>
public sealed record RepricedVariationLine(string ValuationLineItemId, VariationLineInput Line);
