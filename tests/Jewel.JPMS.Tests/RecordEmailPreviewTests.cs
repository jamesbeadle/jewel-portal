using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Four records have their email written for them, so the person pressing Send had never read a
// word of it (2026-09-18). The preview exists so a director can sign off content the product
// finally displays — and it reports the message the door would stage, not a second composition of
// it, which is the whole reason the composer sits between them.
public sealed class RecordEmailPreviewTests
{
    [Fact]
    public async Task APreview_isTheMessageTheDoorWouldStage_cleanedAsItWillLeave()
    {
        var handler = new PreviewRecordEmailHandler(new RecordEmailComposers(new[] { new FakeComposer() }));

        var preview = await handler.HandleAsync(new PreviewRecordEmail(RecordType.Request, "REQ-1"), default);

        Assert.Equal("RFI-052: Shower trays — By France", preview.Subject);
        Assert.Equal("RFI-052", preview.Reference);
        Assert.Equal(new[] { "architect@example.com" }, preview.To);
        Assert.Equal(new[] { "projects@jewelbb.co.uk" }, preview.Cc);

        // What the recipient will read, not what the door happened to hand over.
        Assert.Contains("font-family", preview.BodyHtml);
        Assert.DoesNotContain("script", preview.BodyHtml, StringComparison.OrdinalIgnoreCase);

        var attachment = Assert.Single(preview.Attachments);
        Assert.Equal("RFI-052.pdf", attachment.FileName);
        Assert.Equal(3, attachment.Bytes);
    }

    [Fact]
    public async Task ThePersonsOwnSubjectAndBody_reachTheComposer()
    {
        var composer = new FakeComposer();
        var handler = new PreviewRecordEmailHandler(new RecordEmailComposers(new[] { composer }));

        await handler.HandleAsync(
            new PreviewRecordEmail(RecordType.Request, "REQ-1", "one@example.com", "Mine", "<p>Mine</p>"),
            default);

        Assert.Equal("one@example.com", composer.Asked!.RecipientOverride);
        Assert.Equal("Mine", composer.Asked.Subject);
        Assert.Equal("<p>Mine</p>", composer.Asked.BodyHtml);
    }

    [Fact]
    public async Task ARecordThePortalWritesNoEmailFor_saysSoRatherThanReturningNothing()
    {
        var handler = new PreviewRecordEmailHandler(new RecordEmailComposers(new[] { new FakeComposer() }));

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new PreviewRecordEmail(RecordType.Defect, "DEF-1"), default));

        Assert.Contains("doesn't compose an email", thrown.Message);
    }

    private sealed class FakeComposer : IComposesRecordEmail
    {
        public RecordEmailDraft? Asked { get; private set; }

        public RecordType Record => RecordType.Request;
        public RoleSet RolesThatMaySend => RoleSet.Of(JpmsRoles.ProjectManager);

        public Task<ComposedRecordEmail> ComposeAsync(RecordEmailDraft draft, CancellationToken cancellationToken)
        {
            Asked = draft;
            var message = new MailboxDraftMessage(
                new[] { new MailboxDraftRecipient("architect@example.com") },
                "RFI-052: Shower trays — By France",
                "<div style=\"font-family:Arial\">Please find attached.<script>steal()</script></div>",
                new[] { new MailboxDraftAttachment("RFI-052.pdf", "application/pdf", new byte[] { 1, 2, 3 }) });
            return Task.FromResult(new ComposedRecordEmail(
                "RFI-052", "P-1", message, new[] { "projects@jewelbb.co.uk" }));
        }
    }
}
