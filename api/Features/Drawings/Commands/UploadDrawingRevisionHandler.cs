using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Bluebeam.Extraction;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

/// <summary>
/// Persists a newly uploaded revision as <see cref="DrawingApprovalStatus.Unapproved"/>. It does NOT
/// supersede or touch any sibling revision — that happens only when a revision is approved. Once
/// saved, the revision is queued for transcription (DrawingExtractionAutoQueue) — best effort,
/// after the save, so a queue problem never fails the upload.
/// </summary>
public sealed class UploadDrawingRevisionHandler
    : ICommandHandler<UploadDrawingRevision, DrawingRevision>
{
    private readonly JpmsContext context;
    private readonly AuditActor actor;
    private readonly DrawingExtractionAutoQueue autoQueue;

    public UploadDrawingRevisionHandler(JpmsContext context, AuditActor actor, DrawingExtractionAutoQueue autoQueue)
    {
        this.context = context; this.actor = actor; this.autoQueue = autoQueue;
    }

    public async Task<DrawingRevision> HandleAsync(UploadDrawingRevision command, CancellationToken cancellationToken)
    {
        var drawing = await context.Drawings.FindAsync(new object[] { command.DrawingId }, cancellationToken);
        if (drawing is null) throw new InvalidOperationException($"Document {command.DrawingId} not found.");

        var revision = new DrawingRevisionEntity
        {
            DrawingRevisionId = command.DrawingRevisionId,
            DrawingId = command.DrawingId,
            RevisionLabel = (command.RevisionLabel ?? "").Trim(),
            FileName = command.FileName,
            IssuedByEmail = (command.IssuedByEmail ?? "").Trim(),
            ReceivedAt = DateTimeOffset.UtcNow,
            SupersededAt = null,
            // A blank label is "no revision given", a deliberate choice on upload — not a
            // classification failure, so it does not join the Ambiguous queue.
            IsAmbiguous = false,
            ViewCount = 0,
            ApprovalStatus = (int)DrawingApprovalStatus.Unapproved,
            BlobRef = command.BlobRef,
            ContentType = command.ContentType,
            FileSizeBytes = command.FileSizeBytes
        };

        context.DrawingRevisions.Add(revision);
        await context.SaveChangesAsync(cancellationToken);

        await autoQueue.QueueLandedRevisionAsync(revision, drawing.ProjectId, actor.Email, cancellationToken);
        return revision.ToModel();
    }
}
