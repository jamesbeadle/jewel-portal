using Jewel.JPMS.Api.Features.Progress.WhatsApp;
using Xunit;

namespace Jewel.JPMS.Tests;

// The WhatsApp export as either phone writes it, read into messages (2026-09-16, the FD's
// weekly-report spec, change 3). Pinned: both line formats, media markers, multi-line messages,
// the notices WhatsApp writes itself, and the invisible marks it sprinkles in.
public sealed class WhatsAppExportParserTests
{
    [Fact]
    public void AndroidExport_readsStampSenderTextAndAttachedFile()
    {
        var reading = WhatsAppExportParser.Parse(
            "04/09/2026, 07:58 - James Everitt: Scaffold up to rear elevation\n"
            + "04/09/2026, 08:12 - James Everitt: IMG-20260904-WA0012.jpg (file attached)\n");

        Assert.Equal(2, reading.Messages.Count);
        Assert.Equal(new DateTime(2026, 9, 4, 7, 58, 0), reading.Messages[0].SentAt);
        Assert.Equal("James Everitt", reading.Messages[0].Sender);
        Assert.Equal("Scaffold up to rear elevation", reading.Messages[0].Text);
        Assert.Null(reading.Messages[0].MediaFileName);
        Assert.Equal("IMG-20260904-WA0012.jpg", reading.Messages[1].MediaFileName);
        Assert.Equal("", reading.Messages[1].Text);
    }

    [Fact]
    public void IphoneExport_readsBracketedStampWithSecondsAndAttachedMarker()
    {
        var reading = WhatsAppExportParser.Parse(
            "‎[04/09/2026, 10:22:31] James Everitt: ‎<attached: 00000012-PHOTO-2026-09-04-10-22-31.jpg>\n"
            + "[04/09/2026, 10:23:05] James Everitt: Roof stripped to battens\n");

        Assert.Equal(2, reading.Messages.Count);
        Assert.Equal(new DateTime(2026, 9, 4, 10, 22, 31), reading.Messages[0].SentAt);
        Assert.Equal("00000012-PHOTO-2026-09-04-10-22-31.jpg", reading.Messages[0].MediaFileName);
        Assert.Equal("Roof stripped to battens", reading.Messages[1].Text);
    }

    [Fact]
    public void TwelveHourClock_readsPmAsAfternoon()
    {
        var reading = WhatsAppExportParser.Parse("04/09/26, 2:15 pm - Les Reilly: Ravenswood — soffits on\n");

        Assert.Single(reading.Messages);
        Assert.Equal(new DateTime(2026, 9, 4, 14, 15, 0), reading.Messages[0].SentAt);
        Assert.Equal("Les Reilly", reading.Messages[0].Sender);
    }

    [Fact]
    public void MultiLineMessage_keepsItsContinuationLines()
    {
        var reading = WhatsAppExportParser.Parse(
            "08/09/2026, 16:40 - James Everitt: Today:\n- dormer cheeks framed\n- glazing due Thurs\n"
            + "08/09/2026, 16:41 - James Everitt: All good\n");

        Assert.Equal(2, reading.Messages.Count);
        Assert.Equal("Today:\n- dormer cheeks framed\n- glazing due Thurs", reading.Messages[0].Text);
    }

    [Fact]
    public void WhatsAppsOwnNotices_areDropped_andOmittedMediaIsCounted()
    {
        var reading = WhatsAppExportParser.Parse(
            "01/09/2026, 09:00 - Messages and calls are end-to-end encrypted. No one outside of this chat can read them.\n"
            + "01/09/2026, 09:01 - Nigel Reilly added James Everitt\n"
            + "04/09/2026, 08:12 - James Everitt: <Media omitted>\n");

        Assert.Single(reading.Messages);
        Assert.Equal(1, reading.OmittedMediaCount);
        Assert.Null(reading.Messages[0].MediaFileName);
    }
}
