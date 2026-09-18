using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Api.Features.Requests.Recipients;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed partial class SendRequestEmailHandler
{
    /// <summary>The message the dispatcher will stage: the freshly rendered document, the files
    /// that fit beside it, the cover note, and the tags that make the sent copy file itself.</summary>
    private async Task<MailboxDraftMessage> StagedDraftAsync(
        RequestEntity request,
        RequestDocumentModel model,
        RequestRecipientSet recipients,
        CancellationToken cancellationToken)
    {
        var pdf = RequestDocumentRenderer.Render(model);
        var files = await LoadRequestFileAttachmentsAsync(request.RequestId, cancellationToken);
        var carried = await CarriedOrLinkedAsync(model, pdf, files, cancellationToken);

        return new MailboxDraftMessage(
            recipients.To.Select(ToDraftRecipient).ToList(),
            model.EmailSubject,
            carried.CoverNote,
            carried.Attachments,
            Bcc: recipients.Bcc.Select(ToDraftRecipient).ToList(),
            Categories: await TagsForAsync(request, cancellationToken),
            Cc: recipients.Cc.Select(ToDraftRecipient).ToList());
    }

    /// <summary>The workflow tag rides on the draft and survives the send, so the Sent Items copy
    /// self-associates with the record — and because this document opens a brand-new conversation,
    /// replies to it inherit the tag through the thread sweep instead of waiting in triage. The
    /// Client pathway rides with it so the thread is born on the right side of the Control Centre.
    /// Mirrors the worker's outbound send (MailboxActionWorker.SendRequestDocumentAsync).</summary>
    private async Task<string[]> TagsForAsync(RequestEntity request, CancellationToken cancellationToken) =>
        new[]
        {
            TriageCategories.Marker,
            TriageCategories.ForRecord(await RequestTags.StemAsync(context, request, cancellationToken)),
            TriageCategories.Client
        };

    /// <summary>What actually leaves with the email. The official PDF is ALWAYS attached — it is
    /// the document. Request uploads are allowed up to 64 MB each, well past what one email
    /// carries, so files that would push the message over the Exchange ceiling travel as 7-day
    /// download links in the cover note instead. Linking is best-effort like the file loading
    /// itself: if the share store is unconfigured or a link can't be minted, everything stays
    /// attached exactly as before and the person reviewing the draft sees what they're sending.
    /// </summary>
    private async Task<CarriedFiles> CarriedOrLinkedAsync(
        RequestDocumentModel model,
        byte[] pdf,
        List<MailboxDraftAttachment> files,
        CancellationToken cancellationToken)
    {
        var attachments = new List<MailboxDraftAttachment>
        {
            new(model.FileName, "application/pdf", pdf)
        };
        var coverNote = BuildCoverNote(model);

        var plan = EmailAttachmentPlanner.Split(files, reservedBytes: pdf.LongLength);
        if (plan.ToLink.Count == 0 || !shareStore.IsConfigured)
        {
            attachments.AddRange(files);
            return new CarriedFiles(attachments, coverNote);
        }

        var links = await TryShareAsync(plan.ToLink, $"{model.TypeShort}-{model.DisplayNumber}", cancellationToken);
        if (links is null)
        {
            attachments.AddRange(files); // linking failed — see the note above
            return new CarriedFiles(attachments, coverNote);
        }

        attachments.AddRange(plan.Attach);
        return new CarriedFiles(attachments, coverNote + EmailAttachmentPlanner.LinksHtmlBlock(links));
    }

    /// <summary>What rides on the message, and the cover note that explains it — the two move
    /// together because a file that became a link is named in the words, not the attachment list.</summary>
    private sealed record CarriedFiles(IReadOnlyList<MailboxDraftAttachment> Attachments, string CoverNote);
}
