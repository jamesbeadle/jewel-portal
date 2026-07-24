using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Contract-level guarantees behind the status-pill control on the request and variation pages.
// The variation order is one document (Quoting → Issued → Approved → Rejected) since the
// 2026-07-23 unification. Its status is persisted as an int (and settable directly via
// SetVariationOrderStatus), so renumbering the enum would corrupt stored rows. The
// handler-side transition rules live in the API.
public sealed class VariationStatusTests
{
    [Fact]
    public void VariationOrderStatusEnum_intValuesAreStable()
    {
        // These four values are what the 20260723120000_UnifyVariationOrders migration remapped
        // the old VOQ/VO status pair onto — the migration's data lands on exactly these ints.
        Assert.Equal(0, (int)VariationOrderStatus.Quoting);
        Assert.Equal(1, (int)VariationOrderStatus.Issued);
        Assert.Equal(2, (int)VariationOrderStatus.Approved);
        Assert.Equal(3, (int)VariationOrderStatus.Rejected);
        // Awaiting AI (Architect's Instruction) was appended as 4 — a new pre-approval stage that,
        // like the others, is persisted as an int on the row, so this value must never move.
        Assert.Equal(4, (int)VariationOrderStatus.AwaitingArchitectInstruction);
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
