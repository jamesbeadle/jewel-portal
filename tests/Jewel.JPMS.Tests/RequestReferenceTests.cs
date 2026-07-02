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

    [Fact]
    public void SuggestNext_startsFreshKindAt001EvenInPopulatedRegister()
    {
        // A project's first RFA opens its own sequence — other kinds' numbers don't leak across.
        var existing = new[] { "REQ-0007", "RFI-048", "RFI-049" };
        Assert.Equal("RFA-001", RequestReference.SuggestNext(RequestType.Rfa, existing));
    }

    [Fact]
    public void SuggestNext_ignoresGeneralReqNumbersInRfiSequence()
    {
        // The register mixes General containers (REQ-####, global sequence) with the project's RFIs;
        // the RFI sequence must continue from the highest RFI, not be dragged up by REQ numbers.
        var existing = new[] { "REQ-0120", "REQ-0121", "RFI-012" };
        Assert.Equal("RFI-013", RequestReference.SuggestNext(RequestType.Rfi, existing));
    }

    [Fact]
    public void SuggestNext_continuesAfterBackfilledLegacyNumber()
    {
        // A legacy RFI back-filled under a high original number moves the sequence past it.
        var existing = new[] { "RFI-003", "RFI-100" };
        Assert.Equal("RFI-101", RequestReference.SuggestNext(RequestType.Rfi, existing));
    }
}
