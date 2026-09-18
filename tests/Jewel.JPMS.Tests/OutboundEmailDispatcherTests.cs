using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jewel.JPMS.Tests;

// The one way a record's email leaves the portal (2026-09-17). Nine doors now depend on this
// sequence — stage, send, degrade back to a reviewed draft when the mailbox refuses, audit — and
// until 2026-09-18 nothing pinned it but one save-as-draft case in BidPackageInviteTests. The
// ordering is the point: staging comes first and the send comes last, so an email can never be
// lost between the portal and Outlook.
public sealed partial class OutboundEmailDispatcherTests
{
    private const string StagingRefusal =
        "The email couldn't be staged in the projects mailbox, so nothing was sent.";

    [Fact]
    public async Task ASentEmail_reReadsTheWebLink_andAuditsAgainstItsRecord()
    {
        var fixture = new Fixture();

        var dispatch = await fixture.Dispatcher.DispatchAsync(Draft(), Filing(), saveAsDraftOnly: false, default);

        Assert.True(dispatch.Sent);
        Assert.Null(dispatch.FailureNote);
        Assert.Equal(RecordingMailbox.SentWebLink, dispatch.WebLink);

        var audited = fixture.OnlyAuditRow();
        Assert.Equal((int)AuditEventType.EmailSent, audited.EventType);
        Assert.Equal("RFI-052", audited.RecordReference);
        Assert.Equal("P-1", audited.ProjectId);
        Assert.Equal("Client", audited.Pathway);
        Assert.Equal(RecordingMailbox.SentWebLink, audited.WebLink);
        Assert.Contains("client@example.com", audited.Detail);
    }

    [Fact]
    public async Task ARefusedSend_leavesTheDraftBehind_andSaysWhereToFinishIt()
    {
        var fixture = new Fixture();
        fixture.Mailbox.SendSucceeds = false;

        var dispatch = await fixture.Dispatcher.DispatchAsync(Draft(), Filing(), saveAsDraftOnly: false, default);

        Assert.False(dispatch.Sent);
        Assert.NotNull(dispatch.FailureNote);
        Assert.Contains("Outlook", dispatch.FailureNote!);
        Assert.Equal(RecordingMailbox.DraftWebLink, dispatch.WebLink);
        Assert.Equal((int)AuditEventType.EmailSendFailed, fixture.OnlyAuditRow().EventType);
    }

    [Fact]
    public async Task SaveAsDraftOnly_sendsNothing_andIsRecordedAsADraft()
    {
        var fixture = new Fixture();

        var dispatch = await fixture.Dispatcher.DispatchAsync(Draft(), Filing(), saveAsDraftOnly: true, default);

        Assert.False(fixture.Mailbox.WasSent);
        Assert.False(dispatch.Sent);
        Assert.Null(dispatch.FailureNote);
        Assert.Equal(RecordingMailbox.DraftWebLink, dispatch.WebLink);
        Assert.Equal((int)AuditEventType.DraftCreated, fixture.OnlyAuditRow().EventType);
    }

    private static MailboxDraftMessage Draft() => new(
        new[] { new MailboxDraftRecipient("architect@example.com") },
        "RFI-052: Shower trays",
        "<p>Please find the attached document.</p>",
        Array.Empty<MailboxDraftAttachment>(),
        Cc: new[] { new MailboxDraftRecipient("client@example.com") });

    private static OutboundEmailFiling Filing() => new(
        "Client", StagingRefusal,
        ProjectId: "P-1", RecordType: RecordType.Request, RecordId: "REQ-1", RecordReference: "RFI-052");

    private sealed class Fixture
    {
        public RecordingMailbox Mailbox { get; } = new();
        public JpmsContext Context { get; }
        public OutboundEmailDispatcher Dispatcher { get; }

        public Fixture()
        {
            Context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
                .UseInMemoryDatabase($"dispatch-{Guid.NewGuid():N}").Options);
            Dispatcher = new OutboundEmailDispatcher(
                Mailbox,
                new AuditTrail(Context, new AuditActor { Email = "pm@jewelbb.co.uk" }, NullLogger<AuditTrail>.Instance));
        }

        public AuditEventEntity OnlyAuditRow() => Assert.Single(Context.AuditEvents.ToList());
    }
}
