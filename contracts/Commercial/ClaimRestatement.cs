namespace Jewel.JPMS.Commercial;

/// <summary>
/// The pure half of restating a locked claim's per-line figures: given the claim's frozen rows
/// and the new percentages, works out each row's new money and whether the claim's totals
/// survive. Free of EF/HTTP so the rule can be unit-tested; RestateValuationClaimLinesHandler
/// loads the rows and writes the result onto the entities.
///
/// The rule: a locked claim is what the client was sent and paid, so its money may move BETWEEN
/// its lines but never in or out. Two sums must come back to the penny as they were — the total
/// works complete (the certified figure, and what retention is held on) and the contract-side
/// works (what the deposit release is earned against). A restatement that changes either is a
/// different claim, and is refused.
/// </summary>
public static class ClaimRestatement
{
    /// <summary>One frozen row of the claim as it stands.</summary>
    public sealed record Row(
        string ValuationLineItemId, decimal LineAmount, bool CountsTowardTotals, bool IsVariation, decimal CumulativeClaimed);

    /// <summary>What one row becomes.</summary>
    public sealed record Entry(string ValuationLineItemId, decimal PercentComplete, decimal CumulativeClaimed);

    /// <summary>The restated rows and how much money the restatement would move in or out.</summary>
    public sealed record Outcome(IReadOnlyList<Entry> Entries, decimal TotalMoved, decimal ContractSideMoved)
    {
        public bool KeepsTheTotal => TotalMoved == 0m && ContractSideMoved == 0m;
    }

    /// <param name="rows">The claim's frozen rows for the lines being restated.</param>
    /// <param name="percentByLine">The new cumulative % per line.</param>
    public static Outcome Plan(IReadOnlyList<Row> rows, IReadOnlyDictionary<string, decimal> percentByLine)
    {
        var entries = new List<Entry>(rows.Count);
        var totalMoved = 0m;
        var contractSideMoved = 0m;
        foreach (var row in rows)
        {
            var percent = percentByLine[row.ValuationLineItemId];
            var cumulative = ValuationCalculations.CumulativeClaimed(percent, row.LineAmount);
            entries.Add(new Entry(row.ValuationLineItemId, percent, cumulative));
            if (!row.CountsTowardTotals) continue;
            var moved = cumulative - row.CumulativeClaimed;
            totalMoved += moved;
            if (!row.IsVariation) contractSideMoved += moved;
        }
        return new Outcome(entries, totalMoved, contractSideMoved);
    }
}
