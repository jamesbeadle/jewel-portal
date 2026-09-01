using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings;

/// <summary>
/// The one way a file becomes a drawing revision from bytes: with a drawing id the file joins that
/// drawing; otherwise the drawing is matched by code (case-insensitive) within the project — a new
/// or blank code registers a new drawing — and the file lands as an Unapproved revision, exactly
/// as if uploaded by hand. Code, title and revision label are all optional; a folder applies only
/// to a drawing registered here (an existing drawing stays where it is). Extracted from the retired ImportDrawingFromMessageHandler so Document
/// Control's filing shares its behaviour to the letter. Adds entities and uploads the blob but
/// does NOT save — the caller owns the SaveChanges so its own bookkeeping commits atomically.
/// </summary>
public static class DrawingRevisionLanding
{
    public sealed record Landed(DrawingEntity Drawing, DrawingRevisionEntity Revision);

    public static async Task<Landed> LandAsync(
        JpmsContext context, IDrawingBlobStore blobStore,
        string projectId, string drawingCode, string title, string revisionLabel,
        string fileName, string contentType, byte[] content, string issuedByEmail,
        CancellationToken cancellationToken, string? drawingId = null, string? drawingFolderId = null)
    {
        var drawing = await FindOrRegisterAsync(context, projectId, drawingId, drawingCode, title, drawingFolderId, cancellationToken);

        var revisionId = DrawingIdentifierFactory.NextDrawingRevisionId();
        string blobRef;
        using (var stream = new MemoryStream(content, writable: false))
        {
            blobRef = await blobStore.UploadAsync(
                projectId, drawing.DrawingId, revisionId,
                fileName, contentType, stream, cancellationToken);
        }

        var revision = new DrawingRevisionEntity
        {
            DrawingRevisionId = revisionId,
            DrawingId = drawing.DrawingId,
            RevisionLabel = (revisionLabel ?? "").Trim(),
            FileName = fileName,
            IssuedByEmail = (issuedByEmail ?? "").Trim(),
            ReceivedAt = DateTimeOffset.UtcNow,
            SupersededAt = null,
            // A blank label is "no revision given", not a classification failure.
            IsAmbiguous = false,
            ViewCount = 0,
            ApprovalStatus = (int)DrawingApprovalStatus.Unapproved,
            BlobRef = blobRef,
            ContentType = contentType,
            FileSizeBytes = content.LongLength
        };
        context.DrawingRevisions.Add(revision);
        return new Landed(drawing, revision);
    }

    // A drawing id wins (it is how a revision reaches a drawing that has no code). A blank code can
    // never match an existing drawing — each such file is its own drawing, named by its file until
    // someone gives it a code or title.
    private static async Task<DrawingEntity> FindOrRegisterAsync(
        JpmsContext context, string projectId, string? drawingId, string drawingCode, string title,
        string? drawingFolderId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(drawingId))
        {
            var byId = await context.Drawings.FirstOrDefaultAsync(
                candidate => candidate.DrawingId == drawingId && candidate.ProjectId == projectId, cancellationToken);
            return byId ?? throw new InvalidOperationException("That drawing is no longer on this project.");
        }

        var code = (drawingCode ?? "").Trim();
        var hasCode = code.Length > 0;
        var existing = hasCode
            ? await context.Drawings.FirstOrDefaultAsync(
                candidate => candidate.ProjectId == projectId && candidate.DrawingCode == code, cancellationToken)
            : null;
        if (existing is not null) return existing;

        var drawing = new DrawingEntity
        {
            DrawingId = DrawingIdentifierFactory.NextDrawingId(),
            ProjectId = projectId,
            DrawingCode = code,
            Title = (title ?? "").Trim(),
            CurrentApprovedRevisionLabel = null,
            CreatedAt = DateTimeOffset.UtcNow,
            DrawingFolderId = await FolderOnProjectAsync(context, projectId, drawingFolderId, cancellationToken)
        };
        context.Drawings.Add(drawing);
        return drawing;
    }

    private static async Task<string?> FolderOnProjectAsync(
        JpmsContext context, string projectId, string? drawingFolderId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(drawingFolderId)) return null;
        var isOnProject = await context.DrawingFolders.AsNoTracking().AnyAsync(
            folder => folder.DrawingFolderId == drawingFolderId && folder.ProjectId == projectId, cancellationToken);
        if (!isOnProject) throw new InvalidOperationException("That folder is not on the selected project.");
        return drawingFolderId;
    }
}
