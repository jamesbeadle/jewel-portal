using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Draft work orders: raised with SaveAsDraft, stored unnumbered (Number 0) at status
// Draft, and only given the next sequential number when ApproveWorkOrder fires;
// RejectWorkOrder ends a draft without ever minting a number. The handler-side
// numbering lives in the API; these are the contract- and model-level shapes the UI
// relies on — chiefly that a draft never renders as "WO-0000" and that a rejected
// draft reads as rejected.
public sealed class DraftWorkOrderTests
{
    [Fact]
    public void CreateManualWorkOrder_defaultsToReleased_soExistingCallersAreUntouched()
    {
        var command = new CreateManualWorkOrder(
            "PRJ", "SUB", "Groundworks", "", "fd@jewelgroup.co.uk",
            new[] { new ManualWorkOrderLine("100", "Dig", 1_000m) });

        Assert.False(command.SaveAsDraft);
    }

    [Fact]
    public void DraftOrder_isDraft_andRendersReferenceAsDraft_neverWO0000()
    {
        var draft = OrderWith(number: 0, WorkOrderStatus.Draft);

        Assert.True(draft.IsDraft);
        Assert.Equal("Draft", draft.Reference);
    }

    [Fact]
    public void ApprovedOrder_rendersItsMintedNumber()
    {
        var approved = OrderWith(number: 72, WorkOrderStatus.Released);

        Assert.False(approved.IsDraft);
        Assert.Equal("WO-0072", approved.Reference);
    }

    [Fact]
    public void RejectedDraft_readsRejected_andIsNeitherDraftNorLive()
    {
        var rejected = OrderWith(number: 0, WorkOrderStatus.Rejected);

        Assert.True(rejected.IsRejected);
        Assert.False(rejected.IsDraft);
        Assert.Equal("Rejected", rejected.Reference);
    }

    [Fact]
    public void UnnumberedNonDraft_fallsBackToAnIdStem_matchingTheEntitySideFallback()
    {
        var legacy = OrderWith(number: 0, WorkOrderStatus.Released);

        Assert.Equal("WO-ABCDEF12", legacy.Reference);
    }

    [Fact]
    public void DraftRaisedManually_staysManual_soItCanBeEditedWholesale()
    {
        var draft = OrderWith(number: 0, WorkOrderStatus.Draft);

        Assert.True(draft.IsManual);
    }

    private static WorkOrder OrderWith(int number, WorkOrderStatus status) =>
        new(WorkOrderId: "abcdef12-3456", ProjectId: "PRJ", BidPackageId: null,
            SubcontractorId: "S", Value: 1_000m, Scope: "", AwardedAt: default,
            AwardedByEmail: "", Number: number, Title: "Groundworks", Status: status,
            CreatedAt: default, ScheduledCompletion: null);
}
