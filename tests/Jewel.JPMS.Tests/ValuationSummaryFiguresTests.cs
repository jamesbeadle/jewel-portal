using Jewel.JPMS.Commercial;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The shared summary-footer helper consumed by both the Valuation Report footer and the
// Cashflow tab's retention figures — one computation so the two tabs can't disagree.
public sealed class ValuationSummaryFiguresTests
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

    private static ValuationClaim Claim(
        ValuationClaimStatus status,
        decimal retentionPercent = 5m,
        decimal retentionReleasePercent = 0m,
        decimal totalWorksComplete = 0m,
        decimal retentionHeld = 0m,
        decimal retentionReleased = 0m,
        decimal certifiedToDate = 0m,
        decimal paymentDueExVat = 0m) =>
        new(
            ValuationClaimId: "V1",
            ProjectId: "PRJ-1",
            ClaimNumber: 18,
            ClaimDate: DateTimeOffset.UtcNow,
            Status: status,
            RetentionPercent: retentionPercent,
            RetentionReleasePercent: retentionReleasePercent,
            PreapprovedAt: null,
            ConfirmedAt: null,
            ContractSum: 0m,
            NetVariations: 0m,
            RevisedContractSum: 0m,
            TotalWorksComplete: totalWorksComplete,
            RetentionHeld: retentionHeld,
            RetentionReleased: retentionReleased,
            CertifiedToDate: certifiedToDate,
            PaymentDueExVat: paymentDueExVat);

    // Mirrors the By France Claim 18 shape: retention held at 5% of works complete, release
    // held at £- until a release is separately confirmed (the By France report shows £- here),
    // certified tracking the issued/paid invoices.
    [Fact]
    public void DraftClaim_computesLive_fromPercentCompleteAndInvoicedToDate()
    {
        var lines = new[]
        {
            Line(ValuationElementType.ContractWorks, ValuationLineType.Priced, 1m, 1_780_455m, 1),
            Line(ValuationElementType.Variation, ValuationLineType.Priced, 1m, 215_737.58m, 2),
            Line(ValuationElementType.Variation, ValuationLineType.Tbc, 1m, 38_865m, 3) // never priced in
        };
        var entries = new[]
        {
            new ClaimLine("C1", "V1", "L1", 92m, 0m, 0m),
            new ClaimLine("C2", "V1", "L2", 100m, 0m, 0m)
        };
        var claim = Claim(ValuationClaimStatus.Draft, retentionPercent: 5m, retentionReleasePercent: 2.5m);

        var figures = ValuationSummaryFigures.For(lines, entries, claim, invoicedToDate: 1_513_295.82m);

        Assert.Equal(1_780_455m, figures.ContractSum);
        Assert.Equal(215_737.58m, figures.NetVariations);
        Assert.Equal(1_996_192.58m, figures.RevisedContractSum);

        var worksComplete = 0.92m * 1_780_455m + 215_737.58m;
        Assert.Equal(worksComplete, figures.TotalWorksComplete);
        Assert.Equal(worksComplete * 0.05m, figures.RetentionHeld);
        // Release is a separate confirmed event, never part of a live claim's payment due:
        // 0 until confirmed, matching the frozen total on lock (ValuationClaimSummary).
        Assert.Equal(0m, figures.RetentionReleased);
        Assert.Equal(figures.RetentionHeld - figures.RetentionReleased, figures.RetentionOutstanding);
        Assert.Equal(1_513_295.82m, figures.CertifiedToDate);
        Assert.Equal(
            worksComplete - figures.RetentionHeld + figures.RetentionReleased - 1_513_295.82m,
            figures.PaymentDueExVat);
    }

    [Fact]
    public void LockedClaim_readsFrozenTotals_notLiveEntries()
    {
        var lines = new[] { Line(ValuationElementType.ContractWorks, ValuationLineType.Priced, 1m, 1_780_455m) };
        var claim = Claim(
            ValuationClaimStatus.Preapproved,
            retentionPercent: 5m,
            retentionReleasePercent: 2.5m,
            totalWorksComplete: 1_647_990.65m,
            retentionHeld: 82_399.53m,
            retentionReleased: 49_904.81m,
            certifiedToDate: 1_513_295.82m,
            paymentDueExVat: 102_200.11m);

        // Entries deliberately contradict the frozen totals — they must be ignored.
        var entries = new[] { new ClaimLine("C1", "V1", "L1", 10m, 0m, 0m) };

        var figures = ValuationSummaryFigures.For(lines, entries, claim, invoicedToDate: 999_999m);

        Assert.Equal(1_647_990.65m, figures.TotalWorksComplete);
        Assert.Equal(82_399.53m, figures.RetentionHeld);
        Assert.Equal(49_904.81m, figures.RetentionReleased);
        Assert.Equal(32_494.72m, figures.RetentionOutstanding);
        Assert.Equal(1_513_295.82m, figures.CertifiedToDate);
        Assert.Equal(102_200.11m, figures.PaymentDueExVat);
    }

    [Fact]
    public void NoClaim_zeroRetention_certifiedStillReadsInvoiced()
    {
        var lines = new[] { Line(ValuationElementType.ContractWorks, ValuationLineType.Priced, 1m, 261_218m) };

        var figures = ValuationSummaryFigures.For(lines, Array.Empty<ClaimLine>(), claim: null, invoicedToDate: 0m);

        Assert.Equal(261_218m, figures.ContractSum);
        Assert.Equal(0m, figures.TotalWorksComplete);
        Assert.Equal(0m, figures.RetentionHeld);
        Assert.Equal(0m, figures.RetentionOutstanding);
        Assert.Equal(0m, figures.CertifiedToDate);
        Assert.Equal(0m, figures.PaymentDueExVat);
    }
}
