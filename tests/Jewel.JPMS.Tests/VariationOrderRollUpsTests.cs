using Jewel.JPMS.Api.Features.Commercial.Documents;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Pins the coarser consolidation behind the client's statement (2026-08-26): the PDF and the
// workbook's Summary tab show one row per variation order, every cost centre it touches folded
// in — the screens keep the per-cost-centre rows (VariationRollUpsTests).
public sealed class VariationOrderRollUpsTests
{
    private static ValuationLineItem Line(
        string id, string variationRef, string costCode, decimal amount, int displayOrder,
        ValuationLineType lineType = ValuationLineType.Priced) =>
        new(id, "project", ValuationElementType.Variation, "", "", variationRef, $"{variationRef} title",
            lineType, costCode, $"line {id}", "item", 1m, amount, amount, "", displayOrder);

    [Fact]
    public void Groups_by_variation_only_in_natural_order()
    {
        var lines = new[]
        {
            Line("a", "V10", "0034", 100m, 5),
            Line("b", "V9", "0034", 50m, 6),
            Line("c", "V10", "0051", 200m, 7),
            Line("d", "v10 ", "0034", 30m, 8),
        };

        var rollUps = VariationOrderRollUps.Build(lines);

        Assert.Equal(new[] { "V9", "V10" }, rollUps.Select(rollUp => rollUp.VariationRef));
        Assert.Equal(new[] { "a", "c", "d" }, rollUps[1].Lines.Select(line => line.ValuationLineItemId));
        Assert.Equal(330m, rollUps[1].Amount);
        Assert.False(rollUps[0].IsRolledUp);
        Assert.True(rollUps[1].IsRolledUp);
    }

    [Fact]
    public void Cost_code_is_kept_only_when_every_line_shares_it()
    {
        var oneCentre = VariationOrderRollUps.Build(new[] { Line("a", "V1", "0034", 10m, 1), Line("b", "V1", " 0034", 10m, 2) });
        var mixed = VariationOrderRollUps.Build(new[] { Line("a", "V2", "0034", 10m, 1), Line("b", "V2", "0051", 10m, 2) });

        Assert.Equal("0034", Assert.Single(oneCentre).CostCode);
        Assert.Equal("", Assert.Single(mixed).CostCode);
    }

    [Fact]
    public void Pdf_prints_one_row_per_variation_order_with_summed_money()
    {
        var lines = new[]
        {
            SnapshotLine("a", "V3", "0034", amount: 100m, claimed: 50m, period: 20m, clientReference: "SoW-7"),
            SnapshotLine("b", "V3", "0051", amount: 300m, claimed: 150m, period: 0m, clientReference: "SoW-9"),
            SnapshotLine("c", "V4", "0034", amount: 40m, claimed: 40m, period: 40m, clientReference: "SoW-7"),
        };

        var rows = ValuationReportBillRows.For(lines, ValuationElementType.Variation, _ => null);

        Assert.Equal(2, rows.Count);
        var order = rows[0];
        Assert.Equal("V3", order.Code);
        Assert.Equal("V3 title", order.Title);
        Assert.Equal("2 items", order.Comments);
        Assert.Equal("", order.ClientReference);
        Assert.Equal(400m, order.Amount);
        Assert.Equal(200m, order.CumulativeClaimed);
        Assert.Equal(20m, order.PeriodIncrement);
        Assert.Equal(180m, order.PreviousClaimed);
        Assert.Equal(50m, order.PercentComplete);
        Assert.Equal("SoW-7", rows[1].ClientReference);
    }

    private static ValuationReportSnapshotLine SnapshotLine(
        string id, string variationRef, string costCode, decimal amount, decimal claimed, decimal period, string clientReference) =>
        new(id, "snapshot", id, ValuationElementType.Variation, "", "", variationRef, $"{variationRef} title",
            ValuationLineType.Priced, costCode, "", "item", 1m, amount, amount,
            PercentComplete: amount == 0m ? 0m : claimed / amount * 100m, claimed, period, "", DisplayOrder: 0, clientReference);
}
