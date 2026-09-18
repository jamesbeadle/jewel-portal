using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.MailboxCompose;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

/// <summary>
/// Composes a record's email and describes it instead of sending it. The body is run through the
/// same rule the dispatcher applies on the way out (ComposeHtmlPipeline.FromPortalDocument), so a
/// person reads what the recipient will read and not a draft of it.
///
/// It costs what a send costs up to the point of staging — the document is rendered, the files are
/// read — which is the price of a preview that cannot be wrong.
/// </summary>
public sealed class PreviewRecordEmailHandler : IQueryHandler<PreviewRecordEmail, RecordEmailPreview>
{
    private readonly RecordEmailComposers composers;

    public PreviewRecordEmailHandler(RecordEmailComposers composers) => this.composers = composers;

    public async Task<RecordEmailPreview> HandleAsync(PreviewRecordEmail query, CancellationToken cancellationToken)
    {
        var composed = await composers.For(query.Record).ComposeAsync(
            new RecordEmailDraft(query.RecordId, query.RecipientOverride), cancellationToken);

        return new RecordEmailPreview(
            query.Record,
            query.RecordId,
            composed.Reference,
            composed.Message.Subject,
            ComposeHtmlPipeline.FromPortalDocument(composed.Message.HtmlBody),
            Addresses(composed.Message.To),
            composed.CopiedTo,
            Addresses(composed.Message.Bcc),
            composed.Message.Attachments.Select(Named).ToList());
    }

    private static IReadOnlyList<string> Addresses(IReadOnlyList<MailboxDraftRecipient>? recipients) =>
        (recipients ?? Array.Empty<MailboxDraftRecipient>()).Select(recipient => recipient.Email).ToList();

    /// <summary>An inline image is part of the body, not something the recipient sees listed, so it
    /// is left off — the attachment list is what a person would see in Outlook.</summary>
    private static PreviewedAttachment Named(MailboxDraftAttachment attachment) =>
        new(attachment.FileName, attachment.ContentType, attachment.Content.LongLength);
}
