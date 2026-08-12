using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.DocumentControl.Storage;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

// Copies the ticked attachments of one email into the Document Control queue: bytes mailbox → the
// document-control blob store (a point-in-time copy, so the item outlives the mailbox), envelope
// snapshotted alongside. Runs as part of the email's triage Apply and never consumes the email.
// Attachments already sent from this message are skipped (read-then-insert; the Graph ids are too
// long for a unique SQL index, so this check IS the guarantee) — a re-run Apply cannot double-send.
public sealed class SendAttachmentsToDocumentControlHandler
    : ICommandHandler<SendAttachmentsToDocumentControl, IReadOnlyList<DocumentControlItem>>
{
    private readonly JpmsContext context;
    private readonly IIntakeMessageReader reader;
    private readonly IMailboxGraphClient mailbox;
    private readonly IDocumentControlBlobStore blobStore;
    private readonly AuditActor actor;
    private readonly AuditTrail auditTrail;

    public SendAttachmentsToDocumentControlHandler(
        JpmsContext context, IIntakeMessageReader reader, IMailboxGraphClient mailbox,
        IDocumentControlBlobStore blobStore, AuditActor actor, AuditTrail auditTrail)
    {
        this.context = context; this.reader = reader; this.mailbox = mailbox;
        this.blobStore = blobStore; this.actor = actor; this.auditTrail = auditTrail;
    }

    public async Task<IReadOnlyList<DocumentControlItem>> HandleAsync(
        SendAttachmentsToDocumentControl command, CancellationToken cancellationToken)
    {
        var alreadySent = await context.DocumentControlItems
            .Where(row => row.MessageId == command.MessageId)
            .Select(row => row.AttachmentId)
            .ToListAsync(cancellationToken);

        var freshIds = command.AttachmentIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .Where(id => !alreadySent.Contains(id))
            .ToList();
        if (freshIds.Count == 0) return Array.Empty<DocumentControlItem>();

        // One snapshot serves every attachment of the email; a null snapshot (Graph off, message
        // gone) leaves the envelope blank rather than failing the send — the file is the point.
        var snapshot = await mailbox.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken);

        var created = new List<DocumentControlItemEntity>();
        foreach (var attachmentId in freshIds)
        {
            var attachment = await reader.GetAttachmentAsync(command.MessageId, attachmentId, cancellationToken);
            if (attachment is null)
                throw new InvalidOperationException(
                    "Couldn't download an attachment from the mailbox — it may have been removed, or it isn't a file.");

            var itemId = DocumentControlIdentifierFactory.NextDocumentControlItemId();
            string blobRef;
            using (var stream = new MemoryStream(attachment.Content, writable: false))
            {
                blobRef = await blobStore.UploadItemAsync(
                    itemId, attachment.Name, attachment.ContentType, stream, cancellationToken);
            }

            created.Add(new DocumentControlItemEntity
            {
                DocumentControlItemId = itemId,
                MessageId = command.MessageId,
                InternetMessageId = command.InternetMessageId ?? snapshot?.InternetMessageId,
                AttachmentId = attachmentId,
                FromEmail = snapshot?.FromEmail ?? "",
                FromName = snapshot?.FromName ?? "",
                Subject = snapshot?.Subject ?? "",
                ReceivedAt = snapshot?.ReceivedAt ?? DateTimeOffset.UtcNow,
                FileName = attachment.Name,
                ContentType = attachment.ContentType,
                FileSizeBytes = attachment.Content.LongLength,
                BlobRef = blobRef,
                ProjectIdHint = string.IsNullOrWhiteSpace(command.ProjectIdHint) ? null : command.ProjectIdHint,
                Status = (int)DocumentControlStatus.Pending,
                SentBy = actor.Email,
                SentAt = DateTimeOffset.UtcNow
            });
        }

        context.DocumentControlItems.AddRange(created);
        await context.SaveChangesAsync(cancellationToken);

        foreach (var item in created)
        {
            await auditTrail.WriteAsync(
                AuditEventType.SentToDocumentControl,
                $"Sent \"{item.FileName}\" to Document Control",
                projectId: item.ProjectIdHint,
                emailMessageId: item.MessageId,
                internetMessageId: item.InternetMessageId,
                cancellationToken: cancellationToken);
        }

        return created.Select(entity => entity.ToModel()).ToList();
    }
}
