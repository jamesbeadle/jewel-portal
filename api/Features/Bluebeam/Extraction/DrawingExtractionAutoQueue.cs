using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Bluebeam.Queue;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>
/// Every revision that lands is transcribed (2026-09-16, James: "whenever drawings are triaged,
/// transcribe them into data"). Called by the two landings — Document Triage's filing and the
/// register's hand upload — AFTER their own save, so the revision exists whatever happens here.
/// Best effort by design: the landing has already succeeded, and a queue that is down must never
/// turn a filed drawing into an error. A row that could not be queued is stamped Failed with the
/// reason, so the document page shows it and Extract data is the retry. Non-PDFs are skipped
/// silently — there is nothing to read.
/// </summary>
public sealed class DrawingExtractionAutoQueue
{
    public const string NotQueuedMessage =
        "Automatic transcription couldn't be queued — Extract data on the document page retries it.";

    private readonly JpmsContext context;
    private readonly IDrawingExtractionQueue queue;
    private readonly ILogger<DrawingExtractionAutoQueue> logger;

    public DrawingExtractionAutoQueue(
        JpmsContext context, IDrawingExtractionQueue queue, ILogger<DrawingExtractionAutoQueue> logger)
    {
        this.context = context; this.queue = queue; this.logger = logger;
    }

    public async Task QueueLandedRevisionAsync(
        DrawingRevisionEntity revision, string projectId, string requestedBy, CancellationToken cancellationToken)
    {
        if (!DrawingExtractionQueueing.HasExtractableFile(revision)) return;

        DrawingExtractionEntity? extraction;
        try
        {
            extraction = await StampQueuedRowAsync(revision, projectId, requestedBy, cancellationToken);
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            logger.LogWarning(failure, "Automatic extraction row for revision {RevisionId} could not be written.", revision.DrawingRevisionId);
            return;
        }
        if (extraction is null) return;

        try
        {
            await queue.EnqueueAsync(new DrawingExtractionMessage(revision.DrawingRevisionId, requestedBy), cancellationToken);
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            logger.LogWarning(failure, "Automatic extraction for revision {RevisionId} could not be queued.", revision.DrawingRevisionId);
            await StampNotQueuedAsync(extraction, cancellationToken);
        }
    }

    // Null when a run is already in flight for the revision — nothing to queue.
    private async Task<DrawingExtractionEntity?> StampQueuedRowAsync(
        DrawingRevisionEntity revision, string projectId, string requestedBy, CancellationToken cancellationToken)
    {
        var extraction = await context.DrawingExtractions
            .FirstOrDefaultAsync(row => row.DrawingRevisionId == revision.DrawingRevisionId, cancellationToken);
        if (DrawingExtractionQueueing.IsInFlight(extraction)) return null;
        if (extraction is null)
        {
            extraction = DrawingExtractionQueueing.NewRow(revision.DrawingRevisionId, revision.DrawingId, projectId);
            context.DrawingExtractions.Add(extraction);
        }
        DrawingExtractionQueueing.StampQueued(extraction, requestedBy);
        await context.SaveChangesAsync(cancellationToken);
        return extraction;
    }

    private async Task StampNotQueuedAsync(DrawingExtractionEntity extraction, CancellationToken cancellationToken)
    {
        extraction.Status = (int)DrawingExtractionStatus.Failed;
        extraction.CompletedAt = DateTimeOffset.UtcNow;
        extraction.ErrorMessage = NotQueuedMessage;
        await context.SaveChangesAsync(cancellationToken);
    }
}
