using Jewel.JPMS.Commercial;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The snapshot is a value copy of the report at capture time — its summary must be
// derivable from its own lines with the same ValuationCalculations maths the live
// report uses, so the viewer, the capture handler, and the spreadsheet all agree.
public sealed class ValuationReportSnapshotTests
{
    private static ValuationReportSnapshotLine SnapshotLine(
        ValuationElementType element,
        ValuationLineType type,
        decimal lineAmount,
        decimal percentComplete,
        int order = 1) =>
        new(
            ValuationReportSnapshotLineId: $"SL{order}",
            ValuationReportSnapshotId: "SNAP-1",
            SourceValuationLineItemId: $"L{order}",
            ElementType: element,
            SectionCode: "",
            SectionName: "",
            VariationRef: "",
            VariationTitle: "",
            LineType: type,
            CostCode: "",
            Description: "",
            Unit: "",
            Quantity: 1m,
            Rate: lineAmount,
            LineAmount: lineAmount,
            PercentComplete: percentComplete,
            CumulativeClaimed: ValuationCalculations.CumulativeClaimed(percentComplete, lineAmount),
            PeriodIncrement: 0m,
            Comments: "",
            DisplayOrder: order);

    [Fact]
    public void DeclinedAndTbcLines_neverCountTowardTotals()
    {
        Assert.False(SnapshotLine(ValuationElementType.ContractWorks, ValuationLineType.Declined, 10_000m, 100m).CountsTowardTotals);
        Assert.False(SnapshotLine(ValuationElementType.PcSum, ValuationLineType.Tbc, 10_000m, 100m).CountsTowardTotals);
        Assert.True(SnapshotLine(ValuationElementType.ContractWorks, ValuationLineType.Priced, 10_000m, 100m).CountsTowardTotals);
        Assert.True(SnapshotLine(ValuationElementType.Variation, ValuationLineType.Omit, -10_000m, 100m).CountsTowardTotals);
    }

    // A miniature By France: contract block + a variation omitting a PS and adding a
    // larger supply, partially claimed, against an invoiced history.
    [Fact]
    public void SnapshotSummary_reconcilesFromItsOwnLines()
    {
        var lines = new[]
        {
            SnapshotLine(ValuationElementType.ContractWorks, ValuationLineType.Priced, 1_000_000m, 90m, 1),
            SnapshotLine(ValuationElementType.PcSum, ValuationLineType.ProvisionalSum, 80_000m, 0m, 2),
            SnapshotLine(ValuationElementType.Contingency, ValuationLineType.Priced, 50_000m, 0m, 3),
            SnapshotLine(ValuationElementType.Variation, ValuationLineType.Omit, -80_000m, 100m, 4),
            SnapshotLine(ValuationElementType.Variation, ValuationLineType.Priced, 120_000m, 50m, 5),
            SnapshotLine(ValuationElementType.Variation, ValuationLineType.Declined, 999_999m, 0m, 6) // excluded
        };

        var contractSum = lines
            .Where(line => line.ElementType != ValuationElementType.Variation && line.CountsTowardTotals)
            .Sum(line => line.LineAmount);
        var netVariations = lines
            .Where(line => line.ElementType == ValuationElementType.Variation && line.CountsTowardTotals)
            .Sum(line => line.LineAmount);
        var worksComplete = lines.Where(line => line.CountsTowardTotals).Sum(line => line.CumulativeClaimed);

        Assert.Equal(1_130_000m, contractSum);          // 1,000,000 + 80,000 + 50,000
        Assert.Equal(40_000m, netVariations);           // −80,000 + 120,000
        Assert.Equal(1_170_000m, ValuationCalculations.RevisedContractSum(contractSum, netVariations));
        Assert.Equal(880_000m, worksComplete);          // 900,000 − 80,000 + 60,000

        var retentionHeld = ValuationCalculations.RetentionHeld(worksComplete, 5m);
        var certifiedToDate = 700_000m;                 // Issued+Paid invoices at capture time
        var paymentDue = ValuationCalculations.PaymentDueExVat(worksComplete, retentionHeld, 0m, certifiedToDate);

        Assert.Equal(44_000m, retentionHeld);
        Assert.Equal(136_000m, paymentDue);             // 880,000 − 44,000 − 700,000
    }
}
