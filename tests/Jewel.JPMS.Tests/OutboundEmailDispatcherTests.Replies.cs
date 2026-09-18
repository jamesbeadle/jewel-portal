using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Xunit;

namespace Jewel.JPMS.Tests;

// A reply into an email conversation the record already has, and the one thing a door still owns.
public sealed partial class OutboundEmailDispatcherTests
{
    [Fact]
    public async Task AReply_reportsTheEnvelopeGraphFilledIn_notTheCallersRequest()
    {
        var fixture = new Fixture();

        var dispatch = await fixture.Dispatcher.DispatchReplyAsync(Reply(), Filing(), saveAsDraftOnly: false, default);

        // Graph supplies a reply's recipients and subject from the message being answered, so the
        // caller reports what the correspondent will see rather than what it asked for.
        Assert.Equal("RE: Shower trays", dispatch.Subject);
        Assert.Equal(new[] { "architect@example.com" }, dispatch.To);
        Assert.Equal(new[] { "client@example.com", "projects@jewelbb.co.uk" }, dispatch.Cc);
        Assert.Contains("RE: Shower trays", fixture.OnlyAuditRow().Detail);
    }

    [Fact]
    public async Task AMailboxThatWillNotStageAtAll_throwsTheDoorsOwnSentence_andAuditsNothing()
    {
        var fixture = new Fixture();
        fixture.Mailbox.Staged = null;

        var refused = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Dispatcher.DispatchAsync(Draft(), Filing(), saveAsDraftOnly: false, default));

        // Nothing was staged, so there is nothing to say happened to it — and the sentence the
        // person reads is written at the door they pressed, not here.
        Assert.Equal(StagingRefusal, refused.Message);
        Assert.Empty(fixture.Context.AuditEvents.ToList());
    }

    private static MailboxReplyDraftMessage Reply() => new(
        "inbound-1", "<p>Answering your query.</p>", Array.Empty<MailboxDraftAttachment>());
}
