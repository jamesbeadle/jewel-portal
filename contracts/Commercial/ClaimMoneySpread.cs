namespace Jewel.JPMS.Commercial;

/// <summary>
/// Spreads one sum of certified money across a set of report lines so that every line stands at
/// the same percentage and the lines add back to the sum to the penny.
///
/// This is the maths behind re-shaping a claimed variation's breakdown (VariationClaimRespread):
/// a claim locks the money it certified for a variation, not the shape of the lines beneath it, so
/// when one line becomes nine the claim's money is dealt across the nine in proportion to their
/// amounts. The money column is decimal(18,4) — a raw proportional split can lose or invent a
/// fraction of a penny per line, so the rounding remainder is placed on the line of largest
/// magnitude, and the sum of the spread is always exactly the money that went in.
/// </summary>
public static class ClaimMoneySpread
{
    /// <summary>Scale of every stored money column (decimal(18,4)).</summary>
    public const int MoneyScale = 4;

    /// <summary>The percentage every line stands at when <paramref name="certified"/> is spread across lines totalling <paramref name="total"/>.</summary>
    public static decimal Percent(decimal certified, decimal total) =>
        total == 0m ? 0m : certified / total * 100m;

    /// <summary>
    /// Cumulative money per line: <paramref name="certified"/> in proportion to each line's amount,
    /// rounded to the money scale, the remainder on the line of largest magnitude. A £0 line gets
    /// £0. With no lines nothing is spread; with lines totalling zero the whole sum lands on the
    /// first line so no money is lost.
    /// </summary>
    public static IReadOnlyDictionary<string, decimal> Spread(
        decimal certified, IReadOnlyList<(string LineItemId, decimal Amount)> lines)
    {
        var result = new Dictionary<string, decimal>(lines.Count);
        if (lines.Count == 0) return result;

        var total = lines.Sum(line => line.Amount);
        if (total == 0m)
        {
            foreach (var line in lines) result[line.LineItemId] = 0m;
            result[lines[0].LineItemId] = certified;
            return result;
        }

        var dealt = 0m;
        foreach (var line in lines)
        {
            var share = Math.Round(certified * line.Amount / total, MoneyScale, MidpointRounding.AwayFromZero);
            result[line.LineItemId] = share;
            dealt += share;
        }

        var remainder = certified - dealt;
        if (remainder != 0m)
        {
            var largest = lines.OrderByDescending(line => Math.Abs(line.Amount)).First().LineItemId;
            result[largest] += remainder;
        }
        return result;
    }
}
