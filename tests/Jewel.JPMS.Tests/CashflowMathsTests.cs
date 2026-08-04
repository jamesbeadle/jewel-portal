using Jewel.JPMS.Commercial;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The Cashflow statement's shared maths: left to claim is NET of the retention that future
// valuations will withhold (valuation invoices are raised net of retention, and that slice
// comes back through the release rows instead), and the potential section counts only
// pre-approval variation orders at their estimated values.
public sealed class CashflowMathsTests
{
    // ---- Retention still to withhold -----------------------------------------------------

    [Fact]
    public void RetentionStillToWithhold_isRetentionPercentOfWorksLeftToValue()
    {
        // £2m claim, £1.2m valued: 5% will still be withheld on the £800k left to value.
        Assert.Equal(40_000m, CashflowMaths.RetentionStillToWithhold(2_000_000m, 1_200_000m, 5m));
    }

    [Fact]
    public void RetentionStillToWithhold_zeroPercentMeansNoDeduction()
    {
        // No retention terms on the Setup tab → 0% → the statement behaves as before.
        Assert.Equal(0m, CashflowMaths.RetentionStillToWithhold(2_000_000m, 1_200_000m, 0m));
    }

    [Fact]
    public void RetentionStillToWithhold_neverNegative_whenWorksValuedBeyondTheClaim()
    {
        Assert.Equal(0m, CashflowMaths.RetentionStillToWithhold(2_000_000m, 2_100_000m, 5m));
    }

    [Fact]
    public void RetentionStillToWithhold_fullAtProjectStart_goneAtCompletion()
    {
        Assert.Equal(100_000m, CashflowMaths.RetentionStillToWithhold(2_000_000m, 0m, 5m));
        Assert.Equal(0m, CashflowMaths.RetentionStillToWithhold(2_000_000m, 2_000_000m, 5m));
    }

    // ---- Left to claim -------------------------------------------------------------------

    [Fact]
    public void LeftToClaim_deductsCashAllocatedAndRetentionStillToWithhold()
    {
        var stillToWithhold = CashflowMaths.RetentionStillToWithhold(2_000_000m, 1_200_000m, 5m);

        // £2m claim − £1.08m paid − £60k held (5% x £1.2m valued) − £40k still to withhold.
        Assert.Equal(820_000m, CashflowMaths.LeftToClaim(2_000_000m, 1_080_000m, 60_000m, stillToWithhold));
    }

    // The whole-statement identity the deduction exists for: at any point in the job,
    // left to claim plus BOTH forecast releases plus cash received must equal the project
    // claim — every pound arrives exactly once, none twice.
    [Theory]
    [InlineData(0)]            // day one — nothing valued
    [InlineData(1_200_000)]    // mid-project
    [InlineData(2_000_000)]    // works complete
    public void LeftToClaim_plusReleasesAndCashReceived_alwaysEqualsTheProjectClaim(decimal worksComplete)
    {
        const decimal projectClaim = 2_000_000m;
        const decimal retentionPercent = 5m;

        // Invoiced-and-paid to date: valuations are raised net of retention.
        var cashReceived = worksComplete * (1m - retentionPercent / 100m);
        var retentionOutstanding = worksComplete * retentionPercent / 100m;   // held, nothing released
        var stillToWithhold = CashflowMaths.RetentionStillToWithhold(projectClaim, worksComplete, retentionPercent);
        var leftToClaim = CashflowMaths.LeftToClaim(projectClaim, cashReceived, retentionOutstanding, stillToWithhold);

        // The two releases together return the full 5% pot on the whole claim.
        var releases = projectClaim * retentionPercent / 100m;

        Assert.Equal(projectClaim, leftToClaim + releases + cashReceived);
    }

    [Fact]
    public void LeftToClaim_afterAConfirmedRelease_stillCarriesTheReleasedMoneyUntilPaid()
    {
        // Works complete and fully invoiced: £2m claim, £1.9m paid, release 1 (£50k)
        // confirmed → outstanding drops to £50k, nothing more will be withheld.
        var stillToWithhold = CashflowMaths.RetentionStillToWithhold(2_000_000m, 2_000_000m, 5m);
        Assert.Equal(0m, stillToWithhold);

        // The £50k released has moved into the claim and stays in left to claim until paid.
        Assert.Equal(50_000m, CashflowMaths.LeftToClaim(2_000_000m, 1_900_000m, 50_000m, stillToWithhold));
    }

    // ---- Potential variations ------------------------------------------------------------

    private static VariationOrder Vo(int number, VariationOrderStatus status, decimal? estimate, decimal value = 0m) =>
        new(
            VariationOrderId: $"VO-{number}",
            ProjectId: "PRJ-1",
            RequestId: $"REQ-{number}",
            Number: number,
            Reference: $"VOQ-{number:0000}",
            Title: $"Variation {number}",
            Description: "",
            Status: status,
            SelectedBidPackageId: null,
            SelectedSubcontractorId: null,
            EstimatedValue: estimate,
            VariationRef: null,
            Value: value,
            CostCode: null,
            CreatedAt: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            CreatedByEmail: "qs@jewelgroup.co.uk");

    [Fact]
    public void PotentialVariationValue_countsOnlyPreApprovalOrders_atTheirEstimates()
    {
        var orders = new[]
        {
            Vo(1, VariationOrderStatus.Quoting, 10_000m),
            Vo(2, VariationOrderStatus.Issued, 20_000m),
            Vo(3, VariationOrderStatus.AwaitingArchitectInstruction, 30_000m),
            Vo(4, VariationOrderStatus.Approved, 40_000m, value: 40_000m),  // already in the claim
            Vo(5, VariationOrderStatus.Rejected, 50_000m),                 // gone
        };

        Assert.Equal(60_000m, CashflowMaths.PotentialVariationValue(orders));
    }

    [Fact]
    public void PotentialVariationValue_anOrderWithoutAnEstimateContributesNothing()
    {
        var orders = new[] { Vo(1, VariationOrderStatus.Quoting, null), Vo(2, VariationOrderStatus.Issued, 15_000m) };

        Assert.Equal(15_000m, CashflowMaths.PotentialVariationValue(orders));
    }

    [Fact]
    public void PotentialVariationValue_noOrders_isZero()
    {
        Assert.Equal(0m, CashflowMaths.PotentialVariationValue(Array.Empty<VariationOrder>()));
    }
}
