namespace Jewel.JPMS.Contracts.Xero;

/// <summary>
/// Penny-safe pro-rating: shares a total across weights, rounding each share
/// to 2 dp and giving the final share the remainder so the shares always sum
/// exactly to the total. Used by the Xero write-back to split a line amount
/// across cost centres in the same proportions as the JPMS allocation split —
/// the invoice total must be unchanged to the penny by the split.
/// </summary>
public static class XeroSplitMaths
{
    public static IReadOnlyList<decimal> ProportionalShares(decimal total, IReadOnlyList<decimal> weights)
    {
        if (weights.Count == 0) throw new ArgumentException("At least one weight is required.", nameof(weights));

        var weightSum = weights.Sum();
        if (weightSum == 0m) throw new ArgumentException("Weights must not sum to zero.", nameof(weights));

        var shares = new decimal[weights.Count];
        var allocated = 0m;
        for (var i = 0; i < weights.Count - 1; i++)
        {
            shares[i] = Math.Round(total * weights[i] / weightSum, 2, MidpointRounding.AwayFromZero);
            allocated += shares[i];
        }
        shares[^1] = total - allocated;
        return shares;
    }
}
