using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

// Pulls one attachment out of a triaged email and lands it in the project's drawings. The drawing
// is matched by code (case-insensitive) within the project — a new code registers a new drawing —
// and the file becomes an Unapproved revision, exactly as if it had been uploaded by hand.
// IssuedByEmail records the email's sender (the architect who issued it), not the triager.
public sealed class ImportDrawingFromMessageHandler
    : ICommandHandler<ImportDrawingFromMessage, Drawing>
{
    private readonly JpmsContext context;
    private readonly IIntakeMessageReader reader;
    private readonly IMailboxGraphClient mailbox;
    private readonly IDrawingBlobStore blobStore;

    public ImportDrawingFromMessageHandler(
        JpmsContext context, IIntakeMessageReader reader, IMailboxGraphClient mailbox, IDrawingBlobStore blobStore)
    {
        this.context = context; this.reader = reader; this.mailbox = mailbox; this.blobStore = blobStore;
    }

    public async Task<Drawing> HandleAsync(ImportDrawingFromMessage command, CancellationToken cancellationToken)
    {
        var attachment = await reader.GetAttachmentAsync(command.MessageId, command.AttachmentId, cancellationToken);
        if (attachment is null)
            throw new InvalidOperationException(
                "Couldn't download that attachment from the mailbox — it may have been removed, or it isn't a file.");

        var code = command.DrawingCode.Trim();
        var drawing = await context.Drawings
            .FirstOrDefaultAsync(d => d.ProjectId == command.ProjectId && d.DrawingCode == code, cancellationToken);
        if (drawing is null)
        {
            drawing = new DrawingEntity
            {
                DrawingId = DrawingIdentifierFactory.NextDrawingId(),
                ProjectId = command.ProjectId,
                DrawingCode = code,
                Title = string.IsNullOrWhiteSpace(command.Title) ? code : command.Title.Trim(),
                CurrentApprovedRevisionLabel = null,
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Drawings.Add(drawing);
        }

        var revisionId = DrawingIdentifierFactory.NextDrawingRevisionId();
        string blobRef;
        using (var stream = new MemoryStream(attachment.Content, writable: false))
        {
            blobRef = await blobStore.UploadAsync(
                command.ProjectId, drawing.DrawingId, revisionId,
                attachment.Name, attachment.ContentType, stream, cancellationToken);
        }

        var snapshot = await mailbox.GetSnapshotAsync(command.MessageId, null, cancellationToken);
        var label = command.RevisionLabel.Trim();

        context.DrawingRevisions.Add(new DrawingRevisionEntity
        {
            DrawingRevisionId = revisionId,
            DrawingId = drawing.DrawingId,
            RevisionLabel = label,
            FileName = attachment.Name,
            IssuedByEmail = snapshot?.FromEmail ?? "",
            ReceivedAt = DateTimeOffset.UtcNow,
            SupersededAt = null,
            IsAmbiguous = string.IsNullOrWhiteSpace(label) || label == "?",
            ViewCount = 0,
            ApprovalStatus = (int)DrawingApprovalStatus.Unapproved,
            BlobRef = blobRef,
            ContentType = attachment.ContentType,
            FileSizeBytes = attachment.Content.LongLength
        });

        await context.SaveChangesAsync(cancellationToken);
        return drawing.ToModel();
    }
}
