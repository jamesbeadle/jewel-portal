using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.DocumentControl.Storage;
using Jewel.JPMS.Api.Features.Drawings;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

// Files a pending Document Control item into a project's drawings. Bytes come from the
// document-control blob (not the mailbox — the email may be long gone); the landing itself is the
// shared DrawingRevisionLanding, so the result is indistinguishable from a hand upload.
// IssuedByEmail records the email's snapshotted sender (the architect who issued it), not the filer.
public sealed class FileDocumentAsDrawingHandler
    : ICommandHandler<FileDocumentAsDrawing, DocumentControlItem>
{
    private readonly JpmsContext context;
    private readonly IDocumentControlBlobStore documentBlobs;
    private readonly IDrawingBlobStore drawingBlobs;
    private readonly AuditActor actor;
    private readonly AuditTrail auditTrail;

    public FileDocumentAsDrawingHandler(
        JpmsContext context, IDocumentControlBlobStore documentBlobs, IDrawingBlobStore drawingBlobs,
        AuditActor actor, AuditTrail auditTrail)
    {
        this.context = context; this.documentBlobs = documentBlobs; this.drawingBlobs = drawingBlobs;
        this.actor = actor; this.auditTrail = auditTrail;
    }

    public async Task<DocumentControlItem> HandleAsync(FileDocumentAsDrawing command, CancellationToken cancellationToken)
    {
        var item = await context.DocumentControlItems
            .FirstOrDefaultAsync(row => row.DocumentControlItemId == command.DocumentControlItemId, cancellationToken)
            ?? throw new InvalidOperationException("That document is no longer in Document Triage.");
        if (item.Status != (int)DocumentControlStatus.Pending)
            throw new InvalidOperationException("That document has already been filed or discarded — restore it to the queue first.");

        var project = await context.Projects
            .FirstOrDefaultAsync(row => row.ProjectId == command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException("Select the project the drawing belongs to.");

        var bytes = await ReadItemBytesAsync(item.BlobRef, cancellationToken);

        var landed = await DrawingRevisionLanding.LandAsync(
            context, drawingBlobs,
            command.ProjectId, command.DrawingCode, command.Title, command.RevisionLabel,
            item.FileName, item.ContentType, bytes, item.FromEmail, cancellationToken,
            drawingId: command.DrawingId);

        item.Status = (int)DocumentControlStatus.Filed;
        item.ResolvedBy = actor.Email;
        item.ResolvedAt = DateTimeOffset.UtcNow;
        item.FiledAsKind = (int)DocumentFiledAs.Drawing;
        item.FiledRecordId = landed.Drawing.DrawingId;
        item.FiledLabel = $"Drawing {FiledDrawingName(landed)} on {project.Name}";

        // One save: the drawing, its revision and the item's resolution commit together.
        await context.SaveChangesAsync(cancellationToken);

        await auditTrail.WriteAsync(
            AuditEventType.DocumentFiled,
            $"Filed \"{item.FileName}\" from Document Triage as {item.FiledLabel}",
            projectId: command.ProjectId,
            emailMessageId: item.MessageId,
            internetMessageId: item.InternetMessageId,
            cancellationToken: cancellationToken);

        return item.ToModel();
    }

    // "A-100 Rev C", or the file name when the drawing was filed without a code; the revision is
    // appended only when one was given.
    private static string FiledDrawingName(DrawingRevisionLanding.Landed landed)
    {
        var drawing = landed.Drawing;
        var name = string.IsNullOrWhiteSpace(drawing.DrawingCode) ? landed.Revision.FileName : drawing.DrawingCode;
        var label = landed.Revision.RevisionLabel;
        return string.IsNullOrWhiteSpace(label) ? name : $"{name} Rev {label}";
    }

    private async Task<byte[]> ReadItemBytesAsync(string blobRef, CancellationToken cancellationToken)
    {
        var blob = await documentBlobs.OpenAsync(blobRef, cancellationToken)
            ?? throw new InvalidOperationException("The stored file could not be found in Document Triage's storage.");
        await using var content = blob.Content;
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }
}
