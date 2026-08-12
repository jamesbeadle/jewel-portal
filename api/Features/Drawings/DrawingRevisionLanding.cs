using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings;

/// <summary>
/// The one way a file becomes a drawing revision from bytes: the drawing is matched by code
/// (case-insensitive) within the project — a new code registers a new drawing — and the file lands
/// as an Unapproved revision, exactly as if uploaded by hand. Extracted from the retired
/// ImportDrawingFromMessageHandler so Document Control's filing shares its behaviour to the letter.
/// Adds entities and uploads the blob but does NOT save — the caller owns the SaveChanges so its
/// own bookkeeping commits atomically with the landing.
/// </summary>
public static class DrawingRevisionLanding
{
    public sealed record Landed(DrawingEntity Drawing, DrawingRevisionEntity Revision);

    public static async Task<Landed> LandAsync(
        JpmsContext context, IDrawingBlobStore blobStore,
        string projectId, string drawingCode, string title, string revisionLabel,
        string fileName, string contentType, byte[] content, string issuedByEmail,
        CancellationToken cancellationToken)
    {
        var code = drawingCode.Trim();
        var drawing = await context.Drawings
            .FirstOrDefaultAsync(d => d.ProjectId == projectId && d.DrawingCode == code, cancellationToken);
        if (drawing is null)
        {
            drawing = new DrawingEntity
            {
                DrawingId = DrawingIdentifierFactory.NextDrawingId(),
                ProjectId = projectId,
                DrawingCode = code,
                Title = string.IsNullOrWhiteSpace(title) ? code : title.Trim(),
                CurrentApprovedRevisionLabel = null,
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.Drawings.Add(drawing);
        }

        var revisionId = DrawingIdentifierFactory.NextDrawingRevisionId();
        string blobRef;
        using (var stream = new MemoryStream(content, writable: false))
        {
            blobRef = await blobStore.UploadAsync(
                projectId, drawing.DrawingId, revisionId,
                fileName, contentType, stream, cancellationToken);
        }

        var label = revisionLabel.Trim();
        var revision = new DrawingRevisionEntity
        {
            DrawingRevisionId = revisionId,
            DrawingId = drawing.DrawingId,
            RevisionLabel = label,
            FileName = fileName,
            IssuedByEmail = issuedByEmail,
            ReceivedAt = DateTimeOffset.UtcNow,
            SupersededAt = null,
            IsAmbiguous = string.IsNullOrWhiteSpace(label) || label == "?",
            ViewCount = 0,
            ApprovalStatus = (int)DrawingApprovalStatus.Unapproved,
            BlobRef = blobRef,
            ContentType = contentType,
            FileSizeBytes = content.LongLength
        };
        context.DrawingRevisions.Add(revision);
        return new Landed(drawing, revision);
    }
}
