using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Sales.Inbox;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jewel.JPMS.Tests;

// The sales reply used to stage and send itself and write nothing anywhere (2026-09-18): it was
// the one email the portal sent that left no trace on the audit trail, so a reply from
// sales@jewelbb.co.uk could not afterwards be found by anyone who had not been in the mailbox.
// It now runs the same stage → send → degrade → audit sequence as every other portal email, on a
// dispatcher of its own because it leaves from the second mailbox.
public sealed class SalesOutboundEmailTests
{
    private const string SalesAddress = "sales@jewelbb.co.uk";

    [Fact]
    public async Task AReplyFromALead_isSent_andAuditsAgainstThatLead()
    {
        var fixture = new Fixture();

        var outcome = await fixture.Door.ReplyAsync(
            "message-1", "<p>Thanks for getting in touch.</p>", "LEAD-1", "LD-0007", default);

        Assert.True(outcome.Sent);
        Assert.Contains(SalesAddress, outcome.Message);

        var audited = fixture.OnlyAuditRow();
        Assert.Equal((int)AuditEventType.EmailSent, audited.EventType);
        Assert.Equal("Sales", audited.Pathway);
        Assert.Equal((int)RecordType.Lead, audited.RecordType!.Value);
        Assert.Equal("LEAD-1", audited.RecordId);
        Assert.Equal("LD-0007", audited.RecordReference);
        Assert.Equal(RecordingMailbox.SentWebLink, audited.WebLink);
    }

    [Fact]
    public async Task AReplyWithNoLeadBehindIt_isStillOnTheTrail_underTheSalesPathway()
    {
        var fixture = new Fixture();

        await fixture.Door.ReplyAsync("message-1", "<p>No lead yet.</p>", null, "", default);

        var audited = fixture.OnlyAuditRow();
        Assert.Equal("Sales", audited.Pathway);
        Assert.Null(audited.RecordType);
        Assert.Null(audited.RecordId);
    }

    [Fact]
    public async Task ARefusedSend_namesTheSalesMailbox_ratherThanTheProjectsOne()
    {
        var fixture = new Fixture();
        fixture.Mailbox.SendSucceeds = false;

        var outcome = await fixture.Door.ReplyAsync("message-1", "<p>Refused.</p>", "LEAD-1", "LD-0007", default);

        Assert.False(outcome.Sent);
        Assert.Contains(SalesAddress, outcome.Message);
        Assert.DoesNotContain("projects mailbox", outcome.Message);
        Assert.Equal(RecordingMailbox.DraftWebLink, outcome.DraftWebLink);
        Assert.Equal((int)AuditEventType.EmailSendFailed, fixture.OnlyAuditRow().EventType);
    }

    [Fact]
    public async Task WithNoSalesMailboxConnected_nothingIsSent_andTheReasonIsTheAddress()
    {
        var door = SalesOutboundEmail.NotConnected(SalesAddress);

        var outcome = await door.ReplyAsync("message-1", "<p>Nowhere to go.</p>", "LEAD-1", "LD-0007", default);

        Assert.False(outcome.Sent);
        Assert.Null(outcome.DraftWebLink);
        Assert.Contains(SalesAddress, outcome.Message);
    }

    private sealed class Fixture
    {
        public RecordingMailbox Mailbox { get; } = new();
        public JpmsContext Context { get; }
        public SalesOutboundEmail Door { get; }

        public Fixture()
        {
            Context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
                .UseInMemoryDatabase($"sales-reply-{Guid.NewGuid():N}").Options);
            Door = SalesOutboundEmail.Connected(
                Mailbox,
                SalesAddress,
                new AuditTrail(Context, new AuditActor { Email = "sales@jewelbb.co.uk" }, NullLogger<AuditTrail>.Instance));
        }

        public AuditEventEntity OnlyAuditRow() => Assert.Single(Context.AuditEvents.ToList());
    }
}
