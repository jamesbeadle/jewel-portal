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
        decimal paymentDueExVat = 0m,
        decimal depositPercent = 0m,
        decimal depositReleased = 0m,
        decimal depositReleasedOpening = 0m) =>
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
            PaymentDueExVat: paymentDueExVat,
            DepositPercent: depositPercent,
            DepositReleased: depositReleased,
            DepositReleasedOpening: depositReleasedOpening);

    // Retention held at 5% of works complete; a claim carrying a release % (stamped only
    // once the claim date has reached practical completion) adds the release back into the
    // payment due — the Albany Mews final-account model and PLG's interim-certificate
    // convention (gross less net retention). Certified tracks the issued/paid invoices.
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

        var figures = ValuationSummaryFigures.For(lines, entries, claim, certifiedToDate: 1_513_295.82m);

        Assert.Equal(1_780_455m, figures.ContractSum);
        Assert.Equal(215_737.58m, figures.NetVariations);
        Assert.Equal(1_996_192.58m, figures.RevisedContractSum);

        var worksComplete = 0.92m * 1_780_455m + 215_737.58m;
        Assert.Equal(worksComplete, figures.TotalWorksComplete);
        Assert.Equal(worksComplete * 0.05m, figures.RetentionHeld);
        // The claim carries a 2.5% release (post-practical-completion), so half the
        // retention comes back into the payment due: works less NET retention.
        Assert.Equal(worksComplete * 0.025m, figures.RetentionReleased);
        Assert.Equal(figures.RetentionHeld - figures.RetentionReleased, figures.RetentionOutstanding);
        Assert.Equal(1_513_295.82m, figures.CertifiedToDate);
        Assert.Equal(
            worksComplete - figures.RetentionHeld + figures.RetentionReleased - 1_513_295.82m,
            figures.PaymentDueExVat);
    }

    // A pre-completion claim carries 0% release (StartValuationClaim only stamps the
    // completion release once the claim date reaches practical completion), so the
    // payment due holds the full retention — the By France workbook's £- release row.
    [Fact]
    public void DraftClaim_beforePracticalCompletion_releasesNothing()
    {
        var lines = new[] { Line(ValuationElementType.ContractWorks, ValuationLineType.Priced, 1m, 100_000m) };
        var entries = new[] { new ClaimLine("C1", "V1", "L1", 50m, 0m, 0m) };
        var claim = Claim(ValuationClaimStatus.Draft, retentionPercent: 5m, retentionReleasePercent: 0m);

        var figures = ValuationSummaryFigures.For(lines, entries, claim, certifiedToDate: 0m);

        Assert.Equal(0m, figures.RetentionReleased);
        Assert.Equal(50_000m - 2_500m, figures.PaymentDueExVat);
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

        var figures = ValuationSummaryFigures.For(lines, entries, claim, certifiedToDate: 999_999m);

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

        var figures = ValuationSummaryFigures.For(lines, Array.Empty<ClaimLine>(), claim: null, certifiedToDate: 0m);

        Assert.Equal(261_218m, figures.ContractSum);
        Assert.Equal(0m, figures.TotalWorksComplete);
        Assert.Equal(0m, figures.RetentionHeld);
        Assert.Equal(0m, figures.RetentionOutstanding);
        Assert.Equal(0m, figures.CertifiedToDate);
        Assert.Equal(0m, figures.PaymentDueExVat);
    }

    // The Ravenswood cash-up-front deposit on a live draft: 20% of the contract-side works
    // claimed (variations excluded) is released back and reduces the payment due; the
    // deposit received always reads 20% of the contract sum.
    [Fact]
    public void DraftClaim_depositReleasesAgainstContractSideWorks_andReducesPaymentDue()
    {
        var lines = new[]
        {
            Line(ValuationElementType.ContractWorks, ValuationLineType.Priced, 1m, 261_218m, 1),
            Line(ValuationElementType.Variation, ValuationLineType.Priced, 1m, 19_156.99m, 2)
        };
        var entries = new[]
        {
            new ClaimLine("C1", "V1", "L1", 25m, 0m, 0m),   // 65,304.50 contract-side works
            new ClaimLine("C2", "V1", "L2", 100m, 0m, 0m)   // 19,156.99 variation works
        };
        var claim = Claim(ValuationClaimStatus.Draft, retentionPercent: 5m, depositPercent: 20m);

        var figures = ValuationSummaryFigures.For(lines, entries, claim, certifiedToDate: 10_000m);

        Assert.Equal(20m, figures.DepositPercent);
        Assert.Equal(52_243.60m, figures.DepositReceived);            // 20% × 261,218
        Assert.Equal(13_060.90m, figures.DepositReleased);            // 20% × 65,304.50 — variations excluded
        Assert.Equal(39_182.70m, figures.DepositOutstanding);
        var worksComplete = 65_304.50m + 19_156.99m;
        Assert.Equal(
            worksComplete - figures.RetentionHeld - 13_060.90m - 10_000m,
            figures.PaymentDueExVat);
    }

    // The Ravenswood Claim 3 reconciliation: claims 1–2 were invoiced gross, so their
    // £6,049 of deposit release is an opening balance settled outside the portal. The
    // claim deducts only the release earned beyond it, and the footer shows the workbook's
    // pair: £20,666.77 payable before deposit, £17,916.57 actually invoiced.
    [Fact]
    public void DraftClaim_openingBalance_excludedFromDeduction_matchesRavenswoodWorkbook()
    {
        // Contract-side claimed to 43,996.00 exactly: 100% of a 43,996 line against a
        // 217,222 unclaimed line keeps the contract sum at the workbook's 261,218.
        var linesExact = new[]
        {
            Line(ValuationElementType.ContractWorks, ValuationLineType.Priced, 1m, 43_996m, 1),
            Line(ValuationElementType.ContractWorks, ValuationLineType.Priced, 1m, 217_222m, 2),
            Line(ValuationElementType.Variation, ValuationLineType.Priced, 1m, 19_156.99m, 3)
        };
        var entries = new[]
        {
            new ClaimLine("C1", "V1", "L1", 100m, 0m, 0m),
            new ClaimLine("C2", "V1", "L2", 0m, 0m, 0m),
            new ClaimLine("C3", "V1", "L3", 100m, 0m, 0m)
        };
        var claim = Claim(ValuationClaimStatus.Draft, retentionPercent: 5m,
            depositPercent: 20m, depositReleasedOpening: 6_049.00m);

        var figures = ValuationSummaryFigures.For(linesExact, entries, claim, certifiedToDate: 39_328.57m);

        Assert.Equal(63_152.99m, figures.TotalWorksComplete);
        Assert.Equal(3_157.6495m, figures.RetentionHeld);
        Assert.Equal(52_243.60m, figures.DepositReceived);          // 20% × 261,218
        Assert.Equal(2_750.20m, figures.DepositReleased);           // 8,799.20 earned − 6,049 opening
        Assert.Equal(8_799.20m, figures.DepositReleasedToDate);     // deduction + opening
        Assert.Equal(20_666.77m, decimal.Round(figures.PaymentDueBeforeDepositExVat, 2));
        Assert.Equal(17_916.57m, decimal.Round(figures.PaymentDueExVat, 2));
    }

    // After VI-0003 (17,916.57 cash + 2,750.20 deposit credit) is issued: certification is
    // GROSS (39,328.57 + 20,666.77 = 59,995.34 — the accountant's "39K + 20K"), the credit
    // already taken zeroes the pending deduction, and nothing further is due this period.
    [Fact]
    public void DraftClaim_afterInvoiceIssued_certifiedIsGross_andDeductionReturnsToZero()
    {
        var lines = new[]
        {
            Line(ValuationElementType.ContractWorks, ValuationLineType.Priced, 1m, 43_996m, 1),
            Line(ValuationElementType.ContractWorks, ValuationLineType.Priced, 1m, 217_222m, 2),
            Line(ValuationElementType.Variation, ValuationLineType.Priced, 1m, 19_156.99m, 3)
        };
        var entries = new[]
        {
            new ClaimLine("C1", "V1", "L1", 100m, 0m, 0m),
            new ClaimLine("C2", "V1", "L2", 0m, 0m, 0m),
            new ClaimLine("C3", "V1", "L3", 100m, 0m, 0m)
        };
        var claim = Claim(ValuationClaimStatus.Draft, retentionPercent: 5m,
            depositPercent: 20m, depositReleasedOpening: 6_049.00m);

        var figures = ValuationSummaryFigures.For(lines, entries, claim,
            certifiedToDate: 59_995.34m, depositCreditedToDate: 2_750.20m);

        Assert.Equal(0m, figures.DepositReleased);                  // credit already taken
        Assert.Equal(8_799.20m, figures.DepositReleasedToDate);     // 0 pending + 6,049 opening + 2,750.20 credited
        Assert.Equal(43_444.40m, figures.DepositOutstanding);       // 52,243.60 − 8,799.20
        Assert.Equal(0m, decimal.Round(figures.PaymentDueExVat, 2));
    }

    // A locked claim reads its frozen deposit release; the deposit received stays live
    // against the bill's contract sum.
    [Fact]
    public void LockedClaim_readsFrozenDepositReleased()
    {
        var lines = new[] { Line(ValuationElementType.ContractWorks, ValuationLineType.Priced, 1m, 261_218m) };
        var claim = Claim(
            ValuationClaimStatus.Confirmed,
            totalWorksComplete: 63_152.99m,
            retentionHeld: 3_157.65m,
            certifiedToDate: 39_328.57m,
            paymentDueExVat: 11_365.02m,
            depositPercent: 20m,
            depositReleased: 9_301.75m);

        var figures = ValuationSummaryFigures.For(lines, Array.Empty<ClaimLine>(), claim, certifiedToDate: 0m);

        Assert.Equal(52_243.60m, figures.DepositReceived);
        Assert.Equal(9_301.75m, figures.DepositReleased);
        Assert.Equal(11_365.02m, figures.PaymentDueExVat);
    }
}
