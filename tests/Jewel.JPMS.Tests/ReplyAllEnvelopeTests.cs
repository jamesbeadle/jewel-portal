using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The reply-all envelope get_mailbox_message hands the connector (2026-09-04) — the same prefill
// the Control Centre's Reply box shows, so an AI tool answers from addresses it has actually read.
public sealed class ReplyAllEnvelopeTests
{
    private static MailboxMessageDetail Detail(
        string? from = "craig@wilson.example", string? replyTo = null, string? subject = "Woodhouse Lane",
        IReadOnlyList<string>? to = null, IReadOnlyList<string>? cc = null, string? mailbox = "projects@jewelbb.co.uk") =>
        new("msg-1", "<p>hi</p>", true, Array.Empty<IntakeAttachment>(),
            FromEmail: from, To: to, Cc: cc, ReplyTo: replyTo, Subject: subject, MailboxAddress: mailbox);

    [Fact]
    public void ToIsTheSender_ccIsEveryoneElse_minusTheProjectsMailbox()
    {
        var envelope = ReplyAllEnvelope.For(Detail(
            to: new[] { "projects@jewelbb.co.uk", "nigel@jewelenterprises.example" },
            cc: new[] { "neil@wilson.example", "craig@wilson.example" }));

        Assert.Equal("craig@wilson.example", envelope.To);
        Assert.Equal(new[] { "nigel@jewelenterprises.example", "neil@wilson.example" }, envelope.Cc);
    }

    [Fact]
    public void ReplyToWinsOverFrom_andDuplicatesCollapse()
    {
        var envelope = ReplyAllEnvelope.For(Detail(
            replyTo: "office@wilson.example",
            to: new[] { "Nigel@JewelEnterprises.example" },
            cc: new[] { "nigel@jewelenterprises.example", "OFFICE@wilson.example" }));

        Assert.Equal("office@wilson.example", envelope.To);
        Assert.Equal(new[] { "Nigel@JewelEnterprises.example" }, envelope.Cc);
    }

    [Fact]
    public void SubjectIsPrefixedOnce()
    {
        Assert.Equal("RE: Woodhouse Lane", ReplyAllEnvelope.For(Detail()).Subject);
        Assert.Equal("RE: Woodhouse Lane", ReplyAllEnvelope.For(Detail(subject: "RE: Woodhouse Lane")).Subject);
        Assert.Equal("Re: already", ReplyAllEnvelope.For(Detail(subject: " Re: already ")).Subject);
        Assert.Equal("RE: (no subject)", ReplyAllEnvelope.For(Detail(subject: "  ")).Subject);
    }

    [Fact]
    public void NoReadableSender_leavesToNull()
    {
        var envelope = ReplyAllEnvelope.For(Detail(from: null, mailbox: null));
        Assert.Null(envelope.To);
        Assert.Empty(envelope.Cc);
    }
}
