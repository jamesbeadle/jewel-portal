using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Bluebeam.Queue;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>
/// One extraction, end to end — runs on the worker (the session dance takes minutes; the SWA
/// gateway kills HTTP at ~45s). The local half runs first (PdfPig text + page geometry), then the
/// Bluebeam half: create session → add file → PUT bytes within the upload URL's ten minutes →
/// confirm → read markups; the session is finalised and deleted in a finally so a failed run
/// never leaks one. Idempotent against queue re-delivery: a row already Succeeded is only re-run
/// when the message says Force. Failure stamps the row and rethrows so the queue's retry (5
/// attempts, then poison) is the retry policy.
/// </summary>
public sealed class DrawingExtractionRunner
{
    private readonly JpmsContext context;
    private readonly BluebeamTokenService tokens;
    private readonly IBluebeamClient bluebeam;
    private readonly DrawingExtractionResultWriter resultWriter;
    private readonly IDrawingBlobStore drawingBlobs;
    private readonly ILogger<DrawingExtractionRunner> logger;

    public DrawingExtractionRunner(
        JpmsContext context, BluebeamTokenService tokens, IBluebeamClient bluebeam,
        DrawingExtractionResultWriter resultWriter, IDrawingBlobStore drawingBlobs,
        ILogger<DrawingExtractionRunner> logger)
    {
        this.context = context; this.tokens = tokens; this.bluebeam = bluebeam;
        this.resultWriter = resultWriter; this.drawingBlobs = drawingBlobs; this.logger = logger;
    }

    public async Task RunAsync(DrawingExtractionMessage message, CancellationToken cancellationToken)
    {
        var extraction = await context.DrawingExtractions
            .FirstOrDefaultAsync(row => row.DrawingRevisionId == message.DrawingRevisionId, cancellationToken);
        if (extraction is null)
        {
            logger.LogWarning("Extraction message for revision {RevisionId} has no row — dropped.", message.DrawingRevisionId);
            return;
        }
        if (extraction.Status == (int)DrawingExtractionStatus.Succeeded && !message.Force) return;

        extraction.Status = (int)DrawingExtractionStatus.Running;
        extraction.StartedAt = DateTimeOffset.UtcNow;
        extraction.Attempts += 1;
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            await ExtractAsync(extraction, message, cancellationToken);
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            await resultWriter.RecordFailureAsync(extraction, failure, cancellationToken);
            throw;
        }
    }

    private async Task ExtractAsync(
        DrawingExtractionEntity extraction, DrawingExtractionMessage message, CancellationToken cancellationToken)
    {
        var revision = await context.DrawingRevisions
            .FirstOrDefaultAsync(row => row.DrawingRevisionId == extraction.DrawingRevisionId, cancellationToken)
            ?? throw new InvalidOperationException("The revision no longer exists.");
        var pdfBytes = await ReadRevisionBytesAsync(revision, cancellationToken);

        var textLayer = PdfTextLayerExtractor.Read(pdfBytes);
        var markupsRawJson = await ReadMarkupsThroughBluebeamAsync(extraction, revision, pdfBytes, cancellationToken);

        await resultWriter.RecordSuccessAsync(
            extraction, revision, textLayer, markupsRawJson, message.RequestedBy, cancellationToken);
    }

    private async Task<byte[]> ReadRevisionBytesAsync(
        DrawingRevisionEntity revision, CancellationToken cancellationToken)
    {
        var blob = await drawingBlobs.OpenAsync(revision.BlobRef ?? "", cancellationToken)
            ?? throw new InvalidOperationException("The revision's stored file could not be found.");
        await using var content = blob.Content;
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    private async Task<string> ReadMarkupsThroughBluebeamAsync(
        DrawingExtractionEntity extraction, DrawingRevisionEntity revision, byte[] pdfBytes,
        CancellationToken cancellationToken)
    {
        var accessToken = await tokens.GetAccessTokenAsync(cancellationToken);
        var sessionId = await bluebeam.CreateSessionAsync(
            accessToken, SessionNameFor(revision), cancellationToken);
        extraction.BluebeamSessionId = sessionId;
        try
        {
            var slot = await bluebeam.AddSessionFileAsync(
                accessToken, sessionId, revision.FileName, pdfBytes.LongLength, cancellationToken);
            await bluebeam.UploadFileBytesAsync(slot, pdfBytes, cancellationToken);
            await bluebeam.ConfirmUploadAsync(accessToken, sessionId, slot.FileId, cancellationToken);
            return await bluebeam.GetMarkupsRawJsonAsync(accessToken, sessionId, slot.FileId, cancellationToken);
        }
        finally
        {
            await CleanUpSessionAsync(accessToken, sessionId);
        }
    }

    // Best-effort, and never under the caller's (possibly cancelled) token — a leaked session
    // costs sandbox quota, so the cleanup itself must not be the thing that fails the run.
    private async Task CleanUpSessionAsync(string accessToken, string sessionId)
    {
        try
        {
            await bluebeam.FinalizeSessionAsync(accessToken, sessionId, CancellationToken.None);
            await bluebeam.DeleteSessionAsync(accessToken, sessionId, CancellationToken.None);
        }
        catch (BluebeamCallFailedException failure)
        {
            logger.LogWarning("Bluebeam session {SessionId} could not be cleaned up: {Message}", sessionId, failure.Message);
        }
    }

    private static string SessionNameFor(DrawingRevisionEntity revision) =>
        $"JPMS extract {revision.FileName}".Length <= 60
            ? $"JPMS extract {revision.FileName}"
            : $"JPMS extract {revision.FileName}"[..60];
}
