namespace Jewel.JPMS.Commercial;

/// <summary>
/// The pure half of re-shaping a claimed variation's breakdown: given every claim on the project in
/// number order, the claim entries that stood against the variation's lines before the revision,
/// and the lines as they stand after it, works out what every claim's entries become. Free of
/// EF/HTTP so the rule can be unit-tested; VariationClaimRespread (api) loads the inputs and writes
/// the result onto the entities.
///
/// The rule: a claim locks the MONEY it certified for a variation, never the shape of the lines
/// beneath it. Each claim's entries are read as one sum for the variation and dealt back across
/// the lines the claim now carries — the ones it already had that survived, plus the ones the
/// revision added (a line that was on the report before but which the claim never carried is
/// left alone):
///  • a settled claim (Preapproved / Confirmed) keeps exactly the money it certified, spread in
///    proportion to the lines' amounts, every line at one percentage (ClaimMoneySpread) — one line
///    at 100% becoming nine is nine lines at 100%, and the certified figure does not move by a
///    penny, so a preapproved claim's frozen totals stay true;
///  • the claim still being built (Draft) is the QS's working copy: a re-priced line keeps the
///    percentage that was entered and its money follows the new amount, and a line the revision
///    added joins at the percentage the variation as a whole stood at on that claim;
///  • "this period" is re-derived from the claim immediately before (ClaimPeriodBaseline's rule),
///    using the spread that claim has just been given — a claim that never carried the variation
///    resets the baseline to nothing.
/// </summary>
public static class ClaimRespread
{
    /// <summary>A claim on the project: its id and whether it is still a Draft.</summary>
    public sealed record Claim(string ValuationClaimId, bool IsDraft);

    /// <summary>A claim entry as it stood before the revision.</summary>
    public sealed record PriorEntry(string ValuationClaimId, string ValuationLineItemId, decimal PercentComplete, decimal CumulativeClaimed);

    /// <summary>What one claim's entry for one line becomes; <see cref="IsNew"/> when the claim had no entry for the line.</summary>
    public sealed record Entry(
        string ValuationClaimId, string ValuationLineItemId,
        decimal PercentComplete, decimal CumulativeClaimed, decimal PeriodIncrement, bool IsNew);

    /// <param name="claimsInOrder">Every claim on the project, ascending claim number.</param>
    /// <param name="priorEntries">Every entry that stood against the variation's lines before the revision, dropped lines included.</param>
    /// <param name="lines">The variation's lines after the revision (re-priced and added; dropped ones gone), with their new amounts.</param>
    /// <param name="addedLineIds">Which of <paramref name="lines"/> the revision added.</param>
    /// <param name="oldTotal">The variation's value before the revision.</param>
    public static IReadOnlyList<Entry> Plan(
        IReadOnlyList<Claim> claimsInOrder,
        IReadOnlyList<PriorEntry> priorEntries,
        IReadOnlyList<(string LineItemId, decimal Amount)> lines,
        IReadOnlySet<string> addedLineIds,
        decimal oldTotal)
    {
        var result = new List<Entry>();
        if (priorEntries.Count == 0 || lines.Count == 0) return result;

        var byClaim = priorEntries
            .GroupBy(row => row.ValuationClaimId)
            .ToDictionary(group => group.Key, group => group.ToList());

        IReadOnlyDictionary<string, decimal> previousByLine = new Dictionary<string, decimal>();
        foreach (var claim in claimsInOrder)
        {
            if (!byClaim.TryGetValue(claim.ValuationClaimId, out var entries))
            {
                previousByLine = new Dictionary<string, decimal>();
                continue;
            }

            var certified = entries.Sum(row => row.CumulativeClaimed);
            var carried = entries.ToDictionary(row => row.ValuationLineItemId, row => row);
            var spreadLines = lines
                .Where(line => carried.ContainsKey(line.LineItemId) || addedLineIds.Contains(line.LineItemId))
                .ToList();
            if (spreadLines.Count == 0)
            {
                previousByLine = new Dictionary<string, decimal>();
                continue;
            }

            var percents = new Dictionary<string, decimal>();
            IReadOnlyDictionary<string, decimal> cumulativeByLine;
            if (claim.IsDraft)
            {
                var joiningPercent = ClaimMoneySpread.Percent(certified, oldTotal);
                var draft = new Dictionary<string, decimal>();
                foreach (var line in spreadLines)
                {
                    var percent = carried.TryGetValue(line.LineItemId, out var existing)
                        ? existing.PercentComplete
                        : joiningPercent;
                    percents[line.LineItemId] = percent;
                    draft[line.LineItemId] = ValuationCalculations.CumulativeClaimed(percent, line.Amount);
                }
                cumulativeByLine = draft;
            }
            else
            {
                var percent = ClaimMoneySpread.Percent(certified, spreadLines.Sum(line => line.Amount));
                foreach (var line in spreadLines) percents[line.LineItemId] = percent;
                cumulativeByLine = ClaimMoneySpread.Spread(certified, spreadLines);
            }

            foreach (var line in spreadLines)
            {
                var cumulative = cumulativeByLine[line.LineItemId];
                result.Add(new Entry(
                    claim.ValuationClaimId, line.LineItemId,
                    percents[line.LineItemId], cumulative,
                    cumulative - previousByLine.GetValueOrDefault(line.LineItemId, 0m),
                    IsNew: !carried.ContainsKey(line.LineItemId)));
            }

            previousByLine = cumulativeByLine;
        }

        return result;
    }
}
