using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Bluebeam.Queue;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

// The register's "extract all unprocessed": for every drawing on the project, its latest live
// (non-superseded) PDF revision that has never had metadata extracted and isn't already queued or
// running gets a row + a message. One save for all the rows, then the sends — a message whose row
// write failed must never exist, the reverse is self-healing (the worker re-stamps Queued rows).
public sealed class QueueProjectDrawingExtractionsHandler : ICommandHandler<QueueProjectDrawingExtractions, int>
{
    private readonly JpmsContext context;
    private readonly IDrawingExtractionQueue queue;
    private readonly AuditActor actor;

    public QueueProjectDrawingExtractionsHandler(JpmsContext context, IDrawingExtractionQueue queue, AuditActor actor)
    {
        this.context = context; this.queue = queue; this.actor = actor;
    }

    public async Task<int> HandleAsync(QueueProjectDrawingExtractions command, CancellationToken cancellationToken)
    {
        var candidates = await FindUnprocessedLatestRevisionsAsync(command.ProjectId, cancellationToken);
        if (candidates.Count == 0) return 0;

        var queuedRevisionIds = new List<string>();
        foreach (var revision in candidates)
        {
            var extraction = await context.DrawingExtractions
                .FirstOrDefaultAsync(row => row.DrawingRevisionId == revision.DrawingRevisionId, cancellationToken);
            if (extraction?.Status is (int)DrawingExtractionStatus.Queued or (int)DrawingExtractionStatus.Running)
                continue;

            if (extraction is null)
            {
                extraction = new DrawingExtractionEntity
                {
                    DrawingExtractionId = Guid.NewGuid().ToString("N"),
                    DrawingRevisionId = revision.DrawingRevisionId,
                    DrawingId = revision.DrawingId,
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
            queuedRevisionIds.Add(revision.DrawingRevisionId);
        }
        await context.SaveChangesAsync(cancellationToken);

        foreach (var revisionId in queuedRevisionIds)
            await queue.EnqueueAsync(new DrawingExtractionMessage(revisionId, actor.Email), cancellationToken);
        return queuedRevisionIds.Count;
    }

    private async Task<List<DrawingRevisionEntity>> FindUnprocessedLatestRevisionsAsync(
        string projectId, CancellationToken cancellationToken)
    {
        var drawingIds = await context.Drawings
            .Where(drawing => drawing.ProjectId == projectId)
            .Select(drawing => drawing.DrawingId)
            .ToListAsync(cancellationToken);

        var revisions = await context.DrawingRevisions
            .Where(revision => drawingIds.Contains(revision.DrawingId))
            .Where(revision => revision.SupersededAt == null)
            .Where(revision => revision.MetadataExtractedAt == null)
            .Where(revision => revision.BlobRef != null)
            .ToListAsync(cancellationToken);

        // One candidate per drawing — the newest live revision, PDFs only.
        return revisions
            .Where(IsPdf)
            .GroupBy(revision => revision.DrawingId)
            .Select(group => group.OrderByDescending(revision => revision.ReceivedAt).First())
            .ToList();
    }

    private static bool IsPdf(DrawingRevisionEntity revision) =>
        (revision.ContentType ?? "").Contains("pdf", StringComparison.OrdinalIgnoreCase)
        || revision.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
}
