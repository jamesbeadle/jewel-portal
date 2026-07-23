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
        Assert.Equal(0, (int)RequestStatus.Open);
        Assert.Equal(1, (int)RequestStatus.AwaitingResponse);
        Assert.Equal(2, (int)RequestStatus.Approved);
        Assert.Equal(3, (int)RequestStatus.Rejected);
        Assert.Equal(4, (int)RequestStatus.Closed);
        Assert.Equal(5, (int)RequestStatus.Responded);
    }
}
