using System.Text.Json;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>
/// Persists an extraction's outcome. Success stores both raw payloads as blobs under the
/// revision's own key prefix (markups verbatim, text as [{page,text}] JSON), replaces the
/// revision's markup rows, stamps the extraction row AND the revision's MetadataExtractedAt (the
/// register's badge) in one save, then writes the audit row directly — the worker doesn't link
/// AuditTrail, and audit stays best-effort either way. Failure stamps the row so the UI can show
/// the error the queue's retries keep hitting.
/// </summary>
public sealed class DrawingExtractionResultWriter
{
    private readonly JpmsContext context;
    private readonly IDrawingBlobStore drawingBlobs;
    private readonly ILogger<DrawingExtractionResultWriter> logger;

    public DrawingExtractionResultWriter(
        JpmsContext context, IDrawingBlobStore drawingBlobs, ILogger<DrawingExtractionResultWriter> logger)
    {
        this.context = context; this.drawingBlobs = drawingBlobs; this.logger = logger;
    }

    public async Task RecordSuccessAsync(
        DrawingExtractionEntity extraction, DrawingRevisionEntity revision,
        PdfTextLayerExtractor.TextLayer textLayer, string markupsRawJson, string requestedBy,
        CancellationToken cancellationToken)
    {
        extraction.MarkupsBlobRef = await UploadJsonAsync(
            extraction, "extraction-markups.json", markupsRawJson, cancellationToken);
        extraction.TextBlobRef = await UploadJsonAsync(
            extraction, "extraction-text.json", JsonSerializer.Serialize(textLayer.Pages), cancellationToken);

        var markups = BluebeamMarkupParser.Parse(
            markupsRawJson, extraction.DrawingExtractionId, extraction.DrawingRevisionId);
        var existingRows = await context.DrawingMarkups
            .Where(row => row.DrawingRevisionId == extraction.DrawingRevisionId)
            .ToListAsync(cancellationToken);
        context.DrawingMarkups.RemoveRange(existingRows);
        context.DrawingMarkups.AddRange(markups);

        var now = DateTimeOffset.UtcNow;
        extraction.Status = (int)DrawingExtractionStatus.Succeeded;
        extraction.CompletedAt = now;
        extraction.ErrorMessage = null;
        extraction.PageCount = textLayer.Pages.Count;
        extraction.PagesJson = PdfTextLayerExtractor.GeometryJson(textLayer.Geometry);
        extraction.MarkupCount = markups.Count;
        revision.MetadataExtractedAt = now;
        await context.SaveChangesAsync(cancellationToken);

        await WriteAuditRowAsync(extraction, revision, markups.Count, requestedBy, cancellationToken);
    }

    public async Task RecordFailureAsync(
        DrawingExtractionEntity extraction, Exception failure, CancellationToken cancellationToken)
    {
        // The success path may have staged half its changes on this same context before throwing
        // (markup rows, blob refs, even MetadataExtractedAt). Saving the Failed stamp must not
        // flush those — a row marked Failed that also reads "extracted" would hide the revision
        // from every later bulk queue. Drop the staged state and stamp a freshly-loaded row.
        context.ChangeTracker.Clear();
        var row = await context.DrawingExtractions
            .FirstOrDefaultAsync(
                candidate => candidate.DrawingExtractionId == extraction.DrawingExtractionId,
                CancellationToken.None);
        if (row is null) return;
        row.Status = (int)DrawingExtractionStatus.Failed;
        row.CompletedAt = DateTimeOffset.UtcNow;
        row.ErrorMessage = failure.Message.Length <= 2048 ? failure.Message : failure.Message[..2048];
        row.BluebeamSessionId = extraction.BluebeamSessionId;
        await context.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<string> UploadJsonAsync(
        DrawingExtractionEntity extraction, string fileName, string json, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        return await drawingBlobs.UploadAsync(
            extraction.ProjectId, extraction.DrawingId, extraction.DrawingRevisionId,
            fileName, "application/json", stream, cancellationToken);
    }

    // Best-effort, after the result is safely saved — an audit hiccup must never fail the run.
    private async Task WriteAuditRowAsync(
        DrawingExtractionEntity extraction, DrawingRevisionEntity revision, int markupCount,
        string requestedBy, CancellationToken cancellationToken)
    {
        try
        {
            context.AuditEvents.Add(new AuditEventEntity
            {
                AuditEventId = Guid.NewGuid().ToString("N"),
                OccurredAt = DateTimeOffset.UtcNow,
                ActorEmail = requestedBy,
                EventType = (int)AuditEventType.DrawingDataExtracted,
                Pathway = "",
                ProjectId = extraction.ProjectId,
                RecordReference = "",
                Detail = $"Extracted drawing data from \"{revision.FileName}\" — {markupCount} markup(s)"
            });
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Audit write failed for extraction {ExtractionId}.", extraction.DrawingExtractionId);
        }
    }
}
