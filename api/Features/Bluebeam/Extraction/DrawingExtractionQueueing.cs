using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>
/// The one description of a queued extraction row, shared by the three places that write one:
/// the page's Extract data (QueueDrawingExtractionHandler), the register's extract-all
/// (QueueProjectDrawingExtractionsHandler) and the automatic queue on landing
/// (DrawingExtractionAutoQueue). Only a PDF with a stored file can be read; a revision already
/// Queued or Running is never stacked with a second run.
/// </summary>
public static class DrawingExtractionQueueing
{
    public static bool IsPdf(DrawingRevisionEntity revision) =>
        (revision.ContentType ?? "").Contains("pdf", StringComparison.OrdinalIgnoreCase)
        || revision.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    public static bool HasExtractableFile(DrawingRevisionEntity revision) =>
        !string.IsNullOrWhiteSpace(revision.BlobRef) && IsPdf(revision);

    public static bool IsInFlight(DrawingExtractionEntity? extraction) =>
        extraction?.Status is (int)DrawingExtractionStatus.Queued or (int)DrawingExtractionStatus.Running;

    public static DrawingExtractionEntity NewRow(string revisionId, string drawingId, string projectId) => new()
    {
        DrawingExtractionId = Guid.NewGuid().ToString("N"),
        DrawingRevisionId = revisionId,
        DrawingId = drawingId,
        ProjectId = projectId
    };

    public static void StampQueued(DrawingExtractionEntity extraction, string queuedBy)
    {
        extraction.Status = (int)DrawingExtractionStatus.Queued;
        extraction.QueuedBy = queuedBy;
        extraction.QueuedAt = DateTimeOffset.UtcNow;
        extraction.StartedAt = null;
        extraction.CompletedAt = null;
        extraction.ErrorMessage = null;
    }
}
