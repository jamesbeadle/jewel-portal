using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Api.Features.TenderEnquiries.Documents;
using Jewel.JPMS.Api.Features.Todos;
using Jewel.JPMS.Api.Features.Variations.Documents;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

public sealed partial class SendMailboxEmailHandler
{
    private async Task<List<MailboxDraftAttachment>> ResolveAttachmentsAsync(
        SendMailboxEmail command, IReadOnlyDictionary<string, SendMailboxEmailHandler.UploadedFile>? uploads, CancellationToken ct)
    {
        var resolved = new List<MailboxDraftAttachment>();
        foreach (var reference in command.Attachments ?? Array.Empty<ComposeAttachmentRef>())
        {
            switch (reference.Source)
            {
                case ComposeAttachmentSource.Upload:
                    if (uploads is null || !uploads.TryGetValue(reference.Id, out var file))
                        throw new InvalidOperationException("An attached file didn't arrive with the request — remove it and attach it again.");
                    resolved.Add(new MailboxDraftAttachment(file.FileName, file.ContentType, file.Content));
                    break;

                case ComposeAttachmentSource.Drawing:
                {
                    var revision = await context.DrawingRevisions
                        .FirstOrDefaultAsync(r => r.DrawingRevisionId == reference.Id, ct)
                        ?? throw new InvalidOperationException("A selected drawing revision no longer exists.");
                    var blob = await drawingBlobs.OpenAsync(revision.BlobRef, ct)
                        ?? throw new InvalidOperationException($"The drawing file for {revision.FileName} couldn't be read from storage.");
                    resolved.Add(new MailboxDraftAttachment(
                        revision.FileName, blob.ContentType, await ReadAllAsync(blob.Content, ct)));
                    break;
                }

                case ComposeAttachmentSource.ProgressPhoto:
                {
                    var photo = await context.ProgressPhotos
                        .FirstOrDefaultAsync(p => p.ProgressPhotoId == reference.Id, ct)
                        ?? throw new InvalidOperationException("A selected progress photo no longer exists.");
                    var blob = await photoBlobs.OpenAsync(photo.BlobRef, ct)
                        ?? throw new InvalidOperationException($"The photo file for {photo.FileName} couldn't be read from storage.");
                    resolved.Add(new MailboxDraftAttachment(
                        photo.FileName, blob.ContentType, await ReadAllAsync(blob.Content, ct)));
                    break;
                }

                case ComposeAttachmentSource.RecordDocument:
                {
                    // The record's official PDF, rendered NOW — same builder + renderer as the
                    // record page's download, so the attached file is byte-for-byte the document
                    // as it currently stands. Requests and variation orders carry official
                    // documents; new types slot in as further cases here once they grow a renderer.
                    resolved.Add(await RenderRecordDocumentAsync(reference, ct));
                    break;
                }

                case ComposeAttachmentSource.OriginalMessage:
                {
                    var sourceMessageId = reference.SourceMessageId ?? command.ReplyToMessageId
                        ?? throw new InvalidOperationException("An original-email attachment needs the message it belongs to.");
                    var content = await reader.GetAttachmentAsync(sourceMessageId, reference.Id, ct)
                        ?? throw new InvalidOperationException("An attachment on the original email couldn't be read from the mailbox.");
                    resolved.Add(new MailboxDraftAttachment(content.Name, content.ContentType, content.Content));
                    break;
                }

                default:
                    throw new InvalidOperationException("Unknown attachment source.");
            }
        }
        return resolved;
    }

    // A record's official document, rendered at send time. A null RecordType is a request — the
    // only type the picker offered before variations grew a document (old drafts stay valid).
    private async Task<MailboxDraftAttachment> RenderRecordDocumentAsync(ComposeAttachmentRef reference, CancellationToken ct)
    {
        switch (reference.RecordType)
        {
            case null:
            case RecordType.Request:
            {
                var model = await RequestDocumentBuilder.BuildAsync(context, reference.Id, ct)
                    ?? throw new InvalidOperationException("A selected request record no longer exists — remove its document and try again.");
                return new MailboxDraftAttachment(model.FileName, "application/pdf", RequestDocumentRenderer.Render(model));
            }

            // One variation, two historic identities (VO- and VOQ- tags) — both render the same sheet.
            case RecordType.Variation:
            case RecordType.VariationQuote:
            {
                var model = await VariationDocumentBuilder.BuildAsync(context, reference.Id, ct)
                    ?? throw new InvalidOperationException("A selected variation order no longer exists — remove its document and try again.");
                return new MailboxDraftAttachment(model.FileName, "application/pdf", VariationDocumentRenderer.Render(model));
            }

            // The PQQ response — the architect's questionnaire answered, rendered from the answers
            // as they stand when the email goes.
            case RecordType.TenderEnquiry:
            {
                var model = await TenderEnquiryDocumentBuilder.BuildAsync(context, reference.Id, ct)
                    ?? throw new InvalidOperationException("A selected tender enquiry no longer exists — remove its document and try again.");
                return new MailboxDraftAttachment(model.FileName, "application/pdf", TenderEnquiryDocumentRenderer.Render(model));
            }

            default:
                throw new InvalidOperationException(
                    $"{reference.RecordType} records don't have an official document to attach yet — remove it and try again.");
        }
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream, CancellationToken ct)
    {
        await using (stream)
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            return buffer.ToArray();
        }
    }
}
