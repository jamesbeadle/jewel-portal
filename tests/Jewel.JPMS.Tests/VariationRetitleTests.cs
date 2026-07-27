using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Guarantees behind "retitle a variation". The rule the whole feature rests on: a variation is ONE
// document identified by its NUMBER, so its wording is free to be corrected at any stage without
// anything the client already holds changing meaning. What has been claimed keeps the wording it
// was issued with — the handler writes only the record's own Title — so these tests pin the
// contract's shape rather than a downstream cascade there deliberately isn't one of.
public sealed class VariationRetitleTests
{
    [Fact]
    public void RenameVariationOrder_carriesTheIdAndTitle_andNothingElse()
    {
        // Scope is the point: value, lines and status each have their own command, so a retitle can
        // never be the thing that quietly moved a figure. If a property is ever added here, that
        // promise needs re-reading first.
        var command = new RenameVariationOrder("vo-1", "Kitchen island — stone swap (Calacatta)");

        Assert.Equal("vo-1", command.VariationOrderId);
        Assert.Equal("Kitchen island — stone swap (Calacatta)", command.Title);
        Assert.Equal(2, typeof(RenameVariationOrder).GetProperties().Length);
    }

    [Fact]
    public void Retitling_leavesEveryIdentifierAlone()
    {
        // A user reads "V72" at every stage; the stored Reference keeps its historic VOQ spelling
        // and the V-ref minted at approval is the same number. None of the three is derived from
        // the title, which is exactly why retitling an approved variation is safe.
        var approved = Sample() with { Status = VariationOrderStatus.Approved, VariationRef = "V72", Value = 18_400m };

        var retitled = approved with { Title = "Rooflight upgrade — revised glazing spec" };

        Assert.Equal(approved.Number, retitled.Number);
        Assert.Equal("VOQ-0072", retitled.Reference);
        Assert.Equal("V72", retitled.DisplayNumber);
        Assert.Equal("V72", retitled.VariationRef);
        Assert.Equal(18_400m, retitled.Value);
        Assert.Equal(VariationOrderStatus.Approved, retitled.Status);
    }

    [Fact]
    public void ApprovalAccrualSignature_isKeyedToTheVRef_notTheTitle()
    {
        // The CVR accrual an approval writes reads "{V-ref} — {title as it then read}". Because the
        // title can move afterwards, the un-approve path finds that accrual by its V-REF PREFIX
        // (see ReturnVariationOrderToQuotingHandler); matching on the live title would find nothing
        // after a rename and silently leave the accrual and the budget commitment standing.
        var order = Sample() with { Status = VariationOrderStatus.Approved, VariationRef = "V72" };
        var accrualWrittenAtApproval = $"{order.VariationRef} — {order.Title}";

        var retitled = order with { Title = "Something else entirely" };

        Assert.StartsWith($"{retitled.VariationRef} — ", accrualWrittenAtApproval);
        Assert.DoesNotContain(retitled.Title, accrualWrittenAtApproval);
        // And the separator is what keeps V7 clear of V70.
        Assert.False(accrualWrittenAtApproval.StartsWith("V7 — ", StringComparison.Ordinal));
    }

    private static VariationOrder Sample() => new(
        VariationOrderId: "vo-1",
        ProjectId: "proj-1",
        RequestId: "req-1",
        Number: 72,
        Reference: "VOQ-0072",
        Title: "Kitchen island stone swap",
        Description: "",
        Status: VariationOrderStatus.Quoting,
        SelectedBidPackageId: null,
        SelectedSubcontractorId: null,
        EstimatedValue: null,
        VariationRef: null,
        Value: 0m,
        CostCode: null,
        CreatedAt: DateTimeOffset.UnixEpoch,
        CreatedByEmail: "qs@jewelbb.co.uk");
}
