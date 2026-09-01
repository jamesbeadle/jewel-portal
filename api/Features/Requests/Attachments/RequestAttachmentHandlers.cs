using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Attachments;

internal static class RequestAttachmentMapping
{
    public static RequestAttachment ToModel(this RequestAttachmentEntity entity) =>
        new(
            entity.RequestAttachmentId,
            entity.RequestId,
            entity.ProjectId,
            (RequestAttachmentKind)entity.Kind,
            entity.DrawingId,
            entity.DrawingRevisionId,
            entity.DrawingCode,
            entity.RevisionLabel,
            entity.FileName,
            entity.ContentType,
            entity.FileSizeBytes,
            entity.Caption,
            entity.AddedAt,
            entity.AddedByEmail);

    /// <summary>The request's attachments in the order they were added — the order they were meant
    /// to be read in (photo, then the detail it contradicts).</summary>
    public static async Task<IReadOnlyList<RequestAttachment>> ListAsync(
        JpmsContext context, string requestId, CancellationToken cancellationToken)
    {
        var rows = await context.RequestAttachments
            .AsNoTracking()
            .Where(row => row.RequestId == requestId)
            .OrderBy(row => row.AddedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.ToModel()).ToList();
    }
}

public sealed class ListRequestAttachmentsHandler
    : IQueryHandler<ListRequestAttachments, IReadOnlyList<RequestAttachment>>
{
    private readonly JpmsContext context;
    public ListRequestAttachmentsHandler(JpmsContext context) { this.context = context; }

    public Task<IReadOnlyList<RequestAttachment>> HandleAsync(
        ListRequestAttachments query, CancellationToken cancellationToken) =>
        RequestAttachmentMapping.ListAsync(context, query.RequestId, cancellationToken);
}

/// <summary>
/// Links drawing revisions from the project register onto a request. The revision's code and label
/// are copied onto the link row so the RFI still reads correctly years later, but the link itself
/// points at the register — the drawing is never duplicated into the request.
/// </summary>
public sealed class AttachDrawingsToRequestHandler
    : ICommandHandler<AttachDrawingsToRequest, IReadOnlyList<RequestAttachment>>
{
    private readonly JpmsContext context;
    private readonly Audit.AuditActor actor;

    public AttachDrawingsToRequestHandler(JpmsContext context, Audit.AuditActor actor)
    {
        this.context = context;
        this.actor = actor;
    }

    public async Task<IReadOnlyList<RequestAttachment>> HandleAsync(
        AttachDrawingsToRequest command, CancellationToken cancellationToken)
    {
        var request = await context.Requests
            .FirstOrDefaultAsync(row => row.RequestId == command.RequestId, cancellationToken);
        if (request is null) throw new InvalidOperationException($"Request '{command.RequestId}' not found.");

        var revisionIds = (command.DrawingRevisionIds ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (revisionIds.Count == 0)
            return await RequestAttachmentMapping.ListAsync(context, command.RequestId, cancellationToken);

        // Already-linked revisions are skipped rather than rejected: re-picking from the drawing
        // list is a normal thing to do, and it should not be an error.
        var alreadyLinked = await context.RequestAttachments
            .Where(row => row.RequestId == command.RequestId && row.DrawingRevisionId != null)
            .Select(row => row.DrawingRevisionId!)
            .ToListAsync(cancellationToken);
        var toLink = revisionIds
            .Where(id => !alreadyLinked.Contains(id, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (toLink.Count == 0)
            return await RequestAttachmentMapping.ListAsync(context, command.RequestId, cancellationToken);

        var revisions = await context.DrawingRevisions
            .AsNoTracking()
            .Where(revision => toLink.Contains(revision.DrawingRevisionId))
            .ToListAsync(cancellationToken);

        var drawingIds = revisions.Select(revision => revision.DrawingId).Distinct().ToList();
        var drawings = await context.Drawings
            .AsNoTracking()
            .Where(drawing => drawingIds.Contains(drawing.DrawingId))
            .ToListAsync(cancellationToken);
        var drawingsById = drawings.ToDictionary(drawing => drawing.DrawingId);

        var now = DateTimeOffset.UtcNow;
        foreach (var revision in revisions)
        {
            if (!drawingsById.TryGetValue(revision.DrawingId, out var drawing)) continue;
            // A request can only carry drawings from its own project — an id from elsewhere is a
            // bug or a tampered payload, never a legitimate cross-project reference.
            if (!string.Equals(drawing.ProjectId, request.ProjectId, StringComparison.OrdinalIgnoreCase)) continue;

            context.RequestAttachments.Add(new RequestAttachmentEntity
            {
                RequestAttachmentId = Guid.NewGuid().ToString("N"),
                RequestId = request.RequestId,
                ProjectId = request.ProjectId,
                Kind = (int)RequestAttachmentKind.Drawing,
                DrawingId = drawing.DrawingId,
                DrawingRevisionId = revision.DrawingRevisionId,
                DrawingCode = drawing.DrawingCode,
                RevisionLabel = revision.RevisionLabel,
                AddedAt = now,
                AddedByEmail = actor.Email
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return await RequestAttachmentMapping.ListAsync(context, command.RequestId, cancellationToken);
    }
}

public sealed class RemoveRequestAttachmentHandler
    : ICommandHandler<RemoveRequestAttachment, IReadOnlyList<RequestAttachment>>
{
    private readonly JpmsContext context;
    private readonly IRequestAttachmentStore blobStore;

    public RemoveRequestAttachmentHandler(JpmsContext context, IRequestAttachmentStore blobStore)
    {
        this.context = context;
        this.blobStore = blobStore;
    }

    public async Task<IReadOnlyList<RequestAttachment>> HandleAsync(
        RemoveRequestAttachment command, CancellationToken cancellationToken)
    {
        var entity = await context.RequestAttachments
            .FirstOrDefaultAsync(
                row => row.RequestAttachmentId == command.RequestAttachmentId
                    && row.RequestId == command.RequestId,
                cancellationToken);
        if (entity is null)
            return await RequestAttachmentMapping.ListAsync(context, command.RequestId, cancellationToken);

        var blobRef = entity.BlobRef;
        context.RequestAttachments.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        // Only uploaded files own their bytes — removing a drawing LINK must never touch the
        // register's copy, which other records and the drawings tab still rely on.
        if (entity.Kind == (int)RequestAttachmentKind.File && !string.IsNullOrWhiteSpace(blobRef))
        {
            try { await blobStore.DeleteAsync(blobRef, cancellationToken); }
            catch (Exception ex) when (ex is not OperationCanceledException) { }
        }

        return await RequestAttachmentMapping.ListAsync(context, command.RequestId, cancellationToken);
    }
}
