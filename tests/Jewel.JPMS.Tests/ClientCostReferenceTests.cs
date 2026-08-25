using Jewel.JPMS.Api.Features.Commercial.Documents;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The client's schedule-of-works references on the valuation report PDF (2026-08-25): the
// "Client ref" column exists only when the statement carries at least one reference — every
// other client's PDF keeps the layout it always had — and, when it does, every column after
// Code shifts right by one and the table still fills the A4 text width exactly.
public sealed class ClientCostReferenceTests
{
    private const double TextWidthCentimetres = 17.8;

    private static ValuationReportSnapshotLine Line(string clientReference, int order = 1) =>
        new(
            ValuationReportSnapshotLineId: $"SL{order}",
            ValuationReportSnapshotId: "SNAP-1",
            SourceValuationLineItemId: $"L{order}",
            ElementType: ValuationElementType.ContractWorks,
            SectionCode: "", SectionName: "", VariationRef: "", VariationTitle: "",
            LineType: ValuationLineType.Priced,
            CostCode: "CARP", Description: "Timber frame", Unit: "item",
            Quantity: 1m, Rate: 1_000m, LineAmount: 1_000m,
            PercentComplete: 50m,
            CumulativeClaimed: ValuationCalculations.CumulativeClaimed(50m, 1_000m),
            PeriodIncrement: 0m, Comments: "", DisplayOrder: order,
            ClientReference: clientReference);

    [Fact]
    public void SnapshotLine_defaultsToNoClientReference_soOlderCallersStillCompile()
    {
        Assert.Equal("", Line("").ClientReference);
    }

    [Fact]
    public void NoReferences_keepsTheOriginalLayout()
    {
        var columns = ValuationReportBillColumns.For(new[] { Line(""), Line("   ", 2) });

        Assert.False(columns.HasClientReference);
        Assert.Equal(0, columns.Code);
        Assert.Equal(1, columns.Description);
        Assert.Equal(8, columns.Claimed);
        Assert.Equal(8, columns.Last);
    }

    [Fact]
    public void AnyReference_addsTheColumnAfterCode_andShiftsTheRest()
    {
        var columns = ValuationReportBillColumns.For(new[] { Line(""), Line("3.12", 2) });

        Assert.True(columns.HasClientReference);
        Assert.Equal(0, columns.Code);
        Assert.Equal(1, columns.ClientReference);
        Assert.Equal(2, columns.Description);
        Assert.Equal(3, columns.Quantity);
        Assert.Equal(9, columns.Claimed);
        Assert.Equal(9, columns.Last);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BothLayouts_fillTheTextWidthExactly(bool hasClientReference)
    {
        var columns = new ValuationReportBillColumns(hasClientReference);

        var total = columns.CodeWidthCentimetres
            + (hasClientReference ? columns.ClientReferenceWidthCentimetres : 0)
            + columns.DescriptionWidthCentimetres + columns.QuantityWidthCentimetres
            + columns.RateWidthCentimetres + columns.AmountWidthCentimetres
            + columns.PercentWidthCentimetres + columns.PreviousWidthCentimetres
            + columns.PeriodWidthCentimetres + columns.ClaimedWidthCentimetres;

        Assert.Equal(TextWidthCentimetres, total, precision: 6);
    }
}
