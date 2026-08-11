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
        var paymentDue = ValuationCalculations.PaymentDueExVat(totalWorksComplete, retentionHeld, retentionReleased, 0m, certifiedToDate);
        Assert.Equal(-3_241.87m, decimal.Round(paymentDue, 2));
    }

    // The Ravenswood cash-up-front deposit: 20% of the contract sum received before works
    // start, released back pro rata against the contract-side works so each claim's payment
    // due drops by 20% of what was claimed on contract works + PC sums + contingency.
    [Fact]
    public void Deposit_received_is_deposit_percent_of_the_contract_sum()
    {
        // 20% × £261,218.00 = £52,243.60 — the Ravenswood workbook's deposit.
        Assert.Equal(52_243.60m, ValuationCalculations.DepositReceived(261_218.00m, 20m));
        Assert.Equal(0m, ValuationCalculations.DepositReceived(261_218.00m, 0m));
    }

    [Fact]
    public void Deposit_release_tracks_contract_side_works_and_caps_at_the_deposit_received()
    {
        var received = ValuationCalculations.DepositReceived(261_218.00m, 20m);

        // 20% of the contract-side works claimed comes back to the client each period.
        Assert.Equal(6_049.00m, ValuationCalculations.DepositReleased(30_245.00m, 20m, received));
        Assert.Equal(3_252.75m, ValuationCalculations.DepositReleased(16_263.75m, 20m, received));

        // Even if contract-side works somehow exceed the contract sum, the release stops
        // at the deposit actually received.
        Assert.Equal(received, ValuationCalculations.DepositReleased(300_000.00m, 20m, received));
    }

    [Fact]
    public void Non_variation_works_complete_excludes_variation_and_non_counting_lines()
    {
        var lines = new[]
        {
            Line("L1", ValuationElementType.ContractWorks, ValuationLineType.Priced, 100_000m),
            Line("L2", ValuationElementType.PcSum, ValuationLineType.ProvisionalSum, 20_000m),
            Line("L3", ValuationElementType.Contingency, ValuationLineType.Priced, 10_000m),
            Line("L4", ValuationElementType.Variation, ValuationLineType.Priced, 15_000m),
            Line("L5", ValuationElementType.ContractWorks, ValuationLineType.Declined, 9_999m)
        };
        var claimLines = new[]
        {
            new ClaimLine("C1", "V1", "L1", 50m, 50_000m, 0m),
            new ClaimLine("C2", "V1", "L2", 25m, 5_000m, 0m),
            new ClaimLine("C3", "V1", "L3", 10m, 1_000m, 0m),
            new ClaimLine("C4", "V1", "L4", 100m, 15_000m, 0m),  // variation — excluded
            new ClaimLine("C5", "V1", "L5", 100m, 9_999m, 0m),   // declined — excluded
            new ClaimLine("C6", "V1", "GONE", 100m, 4_444m, 0m)  // line removed — excluded
        };

        Assert.Equal(56_000m, ValuationCalculations.NonVariationWorksComplete(claimLines, lines));
    }

    [Fact]
    public void Payment_due_subtracts_the_deposit_released()
    {
        // 63,152.99 works − 3,157.65 retention − 9,301.75 deposit − 39,328.57 certified.
        var paymentDue = ValuationCalculations.PaymentDueExVat(
            63_152.99m, 3_157.65m, 0m, 9_301.75m, 39_328.57m);
        Assert.Equal(11_365.02m, decimal.Round(paymentDue, 2));
    }

    // The Ravenswood Claim 3 position: claims 1–2 were invoiced gross, so their £6,049
    // of deposit release is settled outside the portal (the opening balance) and the
    // claim only deducts what has been earned beyond it.
    [Fact]
    public void Deposit_deduction_excludes_the_opening_balance()
    {
        // Earned to date 8,799.20 (20% × 43,996 contract-side works), opening 6,049.
        Assert.Equal(2_750.20m, ValuationCalculations.DepositDeduction(8_799.20m, 6_049.00m));

        // An opening balance ahead of the earned release never goes negative — it just
        // waits for the works to catch up.
        Assert.Equal(0m, ValuationCalculations.DepositDeduction(5_000.00m, 6_049.00m));

        // No opening balance = deduct the full earned release.
        Assert.Equal(8_799.20m, ValuationCalculations.DepositDeduction(8_799.20m, 0m));

        // Ravenswood Claim 3 end-to-end: 20,666.77 payable less 2,750.20 = 17,916.57 invoiced.
        var paymentDue = ValuationCalculations.PaymentDueExVat(
            63_152.99m, 3_157.65m, 0m, 2_750.20m, 39_328.57m);
        Assert.Equal(17_916.57m, decimal.Round(paymentDue, 2));
    }

    private static ValuationLineItem Line(
        string id, ValuationElementType elementType, ValuationLineType lineType, decimal amount) =>
        new(id, "P1", elementType, "A1", "Section", "", "", lineType,
            "0001", "Line", "item", 1m, amount, amount, "", 0);
}
