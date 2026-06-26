using Jewel.JPMS.Commercial;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

public sealed class ValuationCalculationsTests
{
    private static ValuationLineItem Line(
        ValuationElementType element,
        ValuationLineType type,
        decimal quantity,
        decimal rate,
        int order = 1) =>
        new(
            ValuationLineItemId: $"L{order}",
            ProjectId: "PRJ-1",
            ElementType: element,
            SectionCode: "",
            SectionName: "",
            VariationRef: "",
            VariationTitle: "",
            LineType: type,
            CostCode: "",
            Description: "",
            Unit: "",
            Quantity: quantity,
            Rate: rate,
            LineAmount: ValuationCalculations.LineAmount(type, quantity, rate),
            Comments: "",
            DisplayOrder: order);

    [Fact]
    public void LineAmount_isQuantityTimesRate() =>
        Assert.Equal(2_500m, ValuationCalculations.LineAmount(ValuationLineType.Priced, 5m, 500m));

    [Fact]
    public void LineAmount_omitIsAlwaysNegative()
    {
        Assert.Equal(-9_359.67m, ValuationCalculations.LineAmount(ValuationLineType.Omit, 1m, 9_359.67m));
        Assert.Equal(-9_359.67m, ValuationCalculations.LineAmount(ValuationLineType.Omit, 1m, -9_359.67m));
    }

    [Fact]
    public void CumulativeClaimed_isPercentOfLineAmount() =>
        Assert.Equal(7_500m, ValuationCalculations.CumulativeClaimed(75m, 10_000m));

    [Fact]
    public void ContractSum_excludesVariationsDeclinedAndTbc()
    {
        var lines = new[]
        {
            Line(ValuationElementType.ContractWorks, ValuationLineType.Priced, 1m, 1_900_000m, 1),
            Line(ValuationElementType.PcSum, ValuationLineType.ProvisionalSum, 1m, 50_000m, 2),
            Line(ValuationElementType.Contingency, ValuationLineType.Priced, 1m, 25_000m, 3),
            Line(ValuationElementType.ContractWorks, ValuationLineType.Declined, 1m, 100_000m, 4), // excluded
            Line(ValuationElementType.PcSum, ValuationLineType.Tbc, 1m, 30_000m, 5),               // excluded
            Line(ValuationElementType.Variation, ValuationLineType.Priced, 1m, 50_000m, 6)         // excluded (variation)
        };

        Assert.Equal(1_975_000m, ValuationCalculations.ContractSum(lines));
    }

    [Fact]
    public void NetVariations_netsOmitsAgainstAdditions()
    {
        var lines = new[]
        {
            Line(ValuationElementType.Variation, ValuationLineType.Priced, 1m, 50_000m, 1),
            Line(ValuationElementType.Variation, ValuationLineType.Omit, 1m, 9_359.67m, 2),
            Line(ValuationElementType.Variation, ValuationLineType.Declined, 1m, 12_000m, 3) // excluded
        };

        Assert.Equal(40_640.33m, ValuationCalculations.NetVariations(lines));
    }

    // Reproduces the By France valuation workbook summary the dashboard replaces.
    [Fact]
    public void WorkedExample_byFranceValuation_matchesWorkbookSummary()
    {
        var lines = new[]
        {
            Line(ValuationElementType.ContractWorks, ValuationLineType.Priced, 1m, 1_900_000m, 1),
            Line(ValuationElementType.PcSum, ValuationLineType.ProvisionalSum, 1m, 50_000m, 2),
            Line(ValuationElementType.Contingency, ValuationLineType.Priced, 1m, 25_000m, 3),
            Line(ValuationElementType.Variation, ValuationLineType.Priced, 1m, 50_000m, 4),
            Line(ValuationElementType.Variation, ValuationLineType.Omit, 1m, 9_359.67m, 5)
        };

        var contractSum = ValuationCalculations.ContractSum(lines);
        var netVariations = ValuationCalculations.NetVariations(lines);
        var revisedContractSum = ValuationCalculations.RevisedContractSum(contractSum, netVariations);

        Assert.Equal(1_975_000m, contractSum);
        Assert.Equal(40_640.33m, netVariations);
        Assert.Equal(2_015_640.33m, revisedContractSum);

        // Works complete this claim — the per-line cumulative claimed sums to the workbook figure.
        var claimLines = new[]
        {
            new ClaimLine("C1", "V1", "L1", 0m, 1_450_000.00m, 0m),
            new ClaimLine("C2", "V1", "L2", 0m,    96_530.47m, 0m),
            new ClaimLine("C3", "V1", "L4", 0m,    43_000.00m, 0m)
        };
        var totalWorksComplete = ValuationCalculations.TotalWorksComplete(claimLines);
        Assert.Equal(1_589_530.47m, totalWorksComplete);

        var retentionHeld = ValuationCalculations.RetentionHeld(totalWorksComplete, 5m);
        Assert.Equal(79_476.52m, decimal.Round(retentionHeld, 2));

        var retentionReleased = ValuationCalculations.RetentionReleased(revisedContractSum, 0m);
        Assert.Equal(0m, retentionReleased);

        const decimal certifiedToDate = 1_513_295.82m; // net certified on the previous confirmed claim
        var paymentDue = ValuationCalculations.PaymentDueExVat(totalWorksComplete, retentionHeld, retentionReleased, certifiedToDate);
        Assert.Equal(-3_241.87m, decimal.Round(paymentDue, 2));
    }
}
