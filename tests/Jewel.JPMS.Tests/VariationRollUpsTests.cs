using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Pins the consolidation rule behind the Variations section of the valuation report (2026-08-25):
// one row per variation order per cost centre, its % complete the weighted result of the lines
// beneath it. Every surface (live table, snapshot viewer, client PDF, Excel) groups through this.
public sealed class VariationRollUpsTests
{
    private static ValuationLineItem Line(
        string id, string variationRef, string costCode, decimal amount, int displayOrder,
        ValuationLineType lineType = ValuationLineType.Priced) =>
        new(id, "project", ValuationElementType.Variation, "", "", variationRef, $"{variationRef} title",
            lineType, costCode, $"line {id}", "item", 1m, amount, amount, "", displayOrder);

    [Fact]
    public void Groups_by_variation_and_cost_centre_in_natural_order()
    {
        var lines = new[]
        {
            Line("a", "V10", "0034", 100m, 5),
            Line("b", "V9", "0034", 50m, 6),
            Line("c", "V10", "0034", 200m, 7),
            Line("d", "V10", "0051", 30m, 8),
            Line("e", "v9", " 0034 ", 25m, 9),
        };

        var rollUps = VariationRollUps.Build(lines);

        Assert.Equal(new[] { "V9|0034", "V10|0034", "V10|0051" }, rollUps.Select(rollUp => rollUp.Key));
        Assert.Equal(new[] { "b", "e" }, rollUps[0].Lines.Select(line => line.ValuationLineItemId));
        Assert.Equal(new[] { "a", "c" }, rollUps[1].Lines.Select(line => line.ValuationLineItemId));
        Assert.True(rollUps[1].IsRolledUp);
        Assert.False(rollUps[2].IsRolledUp);
    }

    [Fact]
    public void Amount_excludes_declined_and_tbc_lines()
    {
        var lines = new[]
        {
            Line("a", "V1", "0034", 100m, 1),
            Line("b", "V1", "0034", 999m, 2, ValuationLineType.Tbc),
            Line("c", "V1", "0034", -40m, 3, ValuationLineType.Omit),
        };

        var rollUp = Assert.Single(VariationRollUps.Build(lines));

        Assert.Equal(60m, rollUp.Amount);
        Assert.Equal(2, rollUp.CountingLines.Count());
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(50, 200, 25)]
    [InlineData(200, 200, 100)]
    [InlineData(-30, 60, -50)]
    [InlineData(1, 3, 33.33)]
    public void Weighted_percent_is_claimed_over_amount(decimal claimed, decimal amount, decimal expected) =>
        Assert.Equal(expected, VariationRollUps.WeightedPercent(claimed, amount));

    [Theory]
    [InlineData("V9", 9)]
    [InlineData("V18", 18)]
    [InlineData("", int.MaxValue)]
    public void Variation_refs_order_by_their_number(string variationRef, int expected) =>
        Assert.Equal(expected, VariationRollUps.VariationRefOrder(variationRef));
}
