using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

public sealed class RequestReferenceTests
{
    [Fact]
    public void SuggestNext_startsAt001WhenRegisterEmpty() =>
        Assert.Equal("RFI-001", RequestReference.SuggestNext(RequestType.Rfi, System.Array.Empty<string>()));

    [Fact]
    public void SuggestNext_incrementsHighestForContiguousRegister()
    {
        var existing = new[] { "RFI-001", "RFI-002", "RFI-003" };
        Assert.Equal("RFI-004", RequestReference.SuggestNext(RequestType.Rfi, existing));
    }

    [Fact]
    public void SuggestNext_usesMaxNotCount_soGapsDoNotCollide()
    {
        // Only three rows survive, but the register has reached RFI-048 — count + 1 would re-issue RFI-004.
        var existing = new[] { "RFI-001", "RFI-002", "RFI-048" };
        Assert.Equal("RFI-049", RequestReference.SuggestNext(RequestType.Rfi, existing));
    }

    [Fact]
    public void SuggestNext_usesMaxNotCount_afterDeletion()
    {
        // RFI-002 deleted: two rows remain but the latest issued was RFI-003.
        var existing = new[] { "RFI-001", "RFI-003" };
        Assert.Equal("RFI-004", RequestReference.SuggestNext(RequestType.Rfi, existing));
    }

    [Fact]
    public void SuggestNext_ignoresFreeTextAndOtherKinds()
    {
        var existing = new[] { "RFI-007", "not a reference", "RFQ-050", "", null };
        Assert.Equal("RFI-008", RequestReference.SuggestNext(RequestType.Rfi, existing));
    }

    [Fact]
    public void SuggestNext_isCaseInsensitiveAndTrimsWhitespace()
    {
        var existing = new[] { "  rfi-012  " };
        Assert.Equal("RFI-013", RequestReference.SuggestNext(RequestType.Rfi, existing));
    }

    [Fact]
    public void SuggestNext_parsesLeadingDigitsOfSuffixedReference()
    {
        var existing = new[] { "RFI-049A" };
        Assert.Equal("RFI-050", RequestReference.SuggestNext(RequestType.Rfi, existing));
    }

    [Fact]
    public void SuggestNext_padsToThreeButKeepsLargerNumbers()
    {
        Assert.Equal("RFI-100", RequestReference.SuggestNext(RequestType.Rfi, new[] { "RFI-099" }));
    }

    [Fact]
    public void SuggestNext_appliesPerKindPrefix() =>
        Assert.Equal("RFQ-006", RequestReference.SuggestNext(RequestType.Rfq, new[] { "RFQ-005" }));
}
