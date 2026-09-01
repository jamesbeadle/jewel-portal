using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Bluebeam.Queue;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

// Queues one revision for extraction. The row is upserted to Queued BEFORE the queue send so the
// UI never sees a queued message with no row behind it; a revision mid-run refuses rather than
// stacking a second run on the same row. Re-queueing a finished revision is the re-extract —
// Force rides on the message so the worker knows the overwrite is meant.
public sealed class QueueDrawingExtractionHandler : ICommandHandler<QueueDrawingExtraction, DrawingExtraction>
{
    private readonly JpmsContext context;
    private readonly IDrawingExtractionQueue queue;
    private readonly AuditActor actor;

    public QueueDrawingExtractionHandler(JpmsContext context, IDrawingExtractionQueue queue, AuditActor actor)
    {
        this.context = context; this.queue = queue; this.actor = actor;
    }

    public async Task<DrawingExtraction> HandleAsync(QueueDrawingExtraction command, CancellationToken cancellationToken)
    {
        var revision = await context.DrawingRevisions
            .FirstOrDefaultAsync(row => row.DrawingRevisionId == command.DrawingRevisionId, cancellationToken)
            ?? throw new InvalidOperationException("That revision no longer exists.");
        if (revision.DrawingId != command.DrawingId)
            throw new InvalidOperationException("That revision belongs to a different drawing.");
        // The extraction row denormalises ProjectId for the register's bulk reads and the audit
        // trail — it comes from the drawing itself, never from the caller.
        var drawing = await context.Drawings
            .FirstOrDefaultAsync(row => row.DrawingId == command.DrawingId, cancellationToken)
            ?? throw new InvalidOperationException("That drawing no longer exists.");
        if (drawing.ProjectId != command.ProjectId)
            throw new InvalidOperationException("That drawing belongs to a different project.");
        if (string.IsNullOrWhiteSpace(revision.BlobRef))
            throw new InvalidOperationException("That revision has no stored file to extract from.");
        var isPdf = (revision.ContentType ?? "").Contains("pdf", StringComparison.OrdinalIgnoreCase)
            || revision.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        if (!isPdf)
            throw new InvalidOperationException("Only PDF revisions can be extracted.");

        var extraction = await context.DrawingExtractions
            .FirstOrDefaultAsync(row => row.DrawingRevisionId == command.DrawingRevisionId, cancellationToken);
        var wasSucceeded = extraction?.Status == (int)DrawingExtractionStatus.Succeeded;
        if (extraction?.Status is (int)DrawingExtractionStatus.Queued or (int)DrawingExtractionStatus.Running)
            throw new InvalidOperationException("That revision is already being extracted — refresh to see where it's up to.");

        if (extraction is null)
        {
            extraction = new DrawingExtractionEntity
            {
                DrawingExtractionId = Guid.NewGuid().ToString("N"),
                DrawingRevisionId = command.DrawingRevisionId,
                DrawingId = command.DrawingId,
                ProjectId = command.ProjectId
            };
            context.DrawingExtractions.Add(extraction);
        }
        extraction.Status = (int)DrawingExtractionStatus.Queued;
        extraction.QueuedBy = actor.Email;
        extraction.QueuedAt = DateTimeOffset.UtcNow;
        extraction.StartedAt = null;
        extraction.CompletedAt = null;
        extraction.ErrorMessage = null;
        await context.SaveChangesAsync(cancellationToken);

        await queue.EnqueueAsync(
            new DrawingExtractionMessage(command.DrawingRevisionId, actor.Email, Force: wasSucceeded),
            cancellationToken);
        return extraction.ToModel();
    }
}
