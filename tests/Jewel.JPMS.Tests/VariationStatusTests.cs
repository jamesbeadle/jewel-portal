using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Contract-level guarantees behind the status-pill controls on the request and VOQ pages.
// Statuses are persisted as ints (and now settable directly via SetVoqStatus /
// RevertVariationOrderToApproved / UpdateRequestDetails), so renumbering any of these enums
// would corrupt stored rows. The handler-side transition rules live in the API.
public sealed class VariationStatusTests
{
    [Fact]
    public void VoqStatusEnum_intValuesAreStable()
    {
        Assert.Equal(0, (int)VariationOrderQuoteStatus.Draft);
        Assert.Equal(1, (int)VariationOrderQuoteStatus.Inviting);
        Assert.Equal(2, (int)VariationOrderQuoteStatus.Tendering);
        Assert.Equal(3, (int)VariationOrderQuoteStatus.Selected);
        Assert.Equal(4, (int)VariationOrderQuoteStatus.Approved);
        Assert.Equal(5, (int)VariationOrderQuoteStatus.Rejected);
    }

    [Fact]
    public void VoStatusEnum_intValuesAreStable()
    {
        Assert.Equal(0, (int)VariationOrderStatus.Approved);
        Assert.Equal(1, (int)VariationOrderStatus.Issued);
        Assert.Equal(2, (int)VariationOrderStatus.Cancelled);
    }

    [Fact]
    public void RequestStatusEnum_intValuesAreStable()
    {
        // 0 and 1 deliberately kept their stored rows across the status consolidation:
        // legacy Open(0) rows now read Needs action, legacy AwaitingResponse(1) rows now read
        // Open. 2/3/5 (Approved/Rejected/Responded) are retired — migrated to Closed — and must
        // never be reused.
        Assert.Equal(0, (int)RequestStatus.NeedsAction);
        Assert.Equal(1, (int)RequestStatus.Open);
        Assert.Equal(4, (int)RequestStatus.Closed);
        Assert.Equal(6, (int)RequestStatus.NeedsVariation);
    }
}
