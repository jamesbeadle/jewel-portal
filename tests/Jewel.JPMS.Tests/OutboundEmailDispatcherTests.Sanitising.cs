using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Xunit;

namespace Jewel.JPMS.Tests;

// The floor: whatever a door hands over, what reaches the mailbox cannot run. Three doors compose
// their body in a textarea of raw HTML, so before 2026-09-18 a script pasted into one of them went
// to a client untouched.
public sealed partial class OutboundEmailDispatcherTests
{
    [Fact]
    public async Task ADraftedBody_reachesTheMailboxWithNothingInItThatCouldRun()
    {
        var fixture = new Fixture();
        var message = Draft() with
        {
            HtmlBody = "<div style=\"font-family:Arial\">Please find attached."
                + "<script>steal()</script><a href=\"javascript:steal()\">here</a></div>"
        };

        await fixture.Dispatcher.DispatchAsync(message, Filing(), saveAsDraftOnly: true, default);

        var staged = fixture.Mailbox.CreatedDraft!.HtmlBody;
        Assert.DoesNotContain("script", staged, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", staged, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("font-family", staged);
        Assert.Contains("Please find attached.", staged);
    }

    [Fact]
    public async Task ARepliedCoverNote_isCleanedTheSameWay()
    {
        var fixture = new Fixture();
        var reply = new MailboxReplyDraftMessage(
            "message-1",
            "<p style=\"margin:0 0 12px\" onclick=\"steal()\">As discussed.</p>",
            Array.Empty<MailboxDraftAttachment>());

        await fixture.Dispatcher.DispatchReplyAsync(reply, Filing(), saveAsDraftOnly: true, default);

        var staged = fixture.Mailbox.CreatedReply!.HtmlCoverNote;
        Assert.DoesNotContain("onclick", staged, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("margin", staged);
        Assert.Contains("As discussed.", staged);
    }
}
