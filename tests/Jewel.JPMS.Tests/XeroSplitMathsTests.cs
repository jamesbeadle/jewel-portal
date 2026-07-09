using Jewel.JPMS.Contracts.Xero;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The Xero write-back splits a line amount across cost centres in the same
/// proportions as the JPMS allocation split; whatever the rounding, the shares
/// must sum back to the original amount to the penny or the invoice total in
/// Xero would drift from what the supplier billed.
/// </summary>
public class XeroSplitMathsTests
{
    [Fact]
    public void EqualWeightsShareEvenly()
    {
        var shares = XeroSplitMaths.ProportionalShares(100m, new[] { 1m, 1m });
        Assert.Equal(new[] { 50m, 50m }, shares);
    }

    [Fact]
    public void SharesFollowTheWeights()
    {
        var shares = XeroSplitMaths.ProportionalShares(1000m, new[] { 750m, 250m });
        Assert.Equal(new[] { 750m, 1000m - 750m }, shares);
    }

    [Fact]
    public void RoundingRemainderGoesToTheLastShare()
    {
        // 100 / 3 rounds to 33.33 each — the last share absorbs the extra penny.
        var shares = XeroSplitMaths.ProportionalShares(100m, new[] { 1m, 1m, 1m });
        Assert.Equal(33.33m, shares[0]);
        Assert.Equal(33.33m, shares[1]);
        Assert.Equal(33.34m, shares[2]);
    }

    [Theory]
    [InlineData(100.00, 3)]
    [InlineData(999.99, 7)]
    [InlineData(0.05, 4)]
    [InlineData(12345.67, 5)]
    public void SharesAlwaysSumExactlyToTheTotal(decimal total, int parts)
    {
        var weights = Enumerable.Range(1, parts).Select(i => (decimal)i).ToArray();
        var shares = XeroSplitMaths.ProportionalShares(total, weights);
        Assert.Equal(total, shares.Sum());
        Assert.Equal(parts, shares.Count);
    }

    [Fact]
    public void VatInclusiveAmountsSplitOnNetWeightsStillSumExactly()
    {
        // The write-back pro-rates the RAW LineAmount (which may include VAT) using
        // the allocation's NET weights — proportions are identical, totals must hold.
        var shares = XeroSplitMaths.ProportionalShares(120.00m, new[] { 66.67m, 33.33m });
        Assert.Equal(120.00m, shares.Sum());
    }

    [Fact]
    public void NegativeTotalsSplitTheSameWay()
    {
        var shares = XeroSplitMaths.ProportionalShares(-100m, new[] { 1m, 1m, 1m });
        Assert.Equal(-100m, shares.Sum());
    }

    [Fact]
    public void ZeroWeightSumIsRejected()
    {
        Assert.Throws<ArgumentException>(() => XeroSplitMaths.ProportionalShares(100m, new[] { 0m, 0m }));
    }

    [Fact]
    public void EmptyWeightsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => XeroSplitMaths.ProportionalShares(100m, Array.Empty<decimal>()));
    }
}
