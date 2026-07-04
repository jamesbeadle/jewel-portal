using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Drawings;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

/// <summary>
/// Hard-deletes a drawing together with all of its revisions and any issue records that referenced
/// them. The database rows are removed first; blob cleanup runs afterwards as best effort, so a
/// storage hiccup can only leave an orphaned (harmless) blob, never a revision pointing at a
/// missing file.
/// </summary>
public sealed class DeleteDrawingHandler : ICommandHandler<DeleteDrawing, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly IDrawingBlobStore blobStore;

    public DeleteDrawingHandler(JpmsContext context, IDrawingBlobStore blobStore)
    {
        this.context = context;
        this.blobStore = blobStore;
    }

    public async Task<Acknowledgement> HandleAsync(DeleteDrawing command, CancellationToken cancellationToken)
    {
        var drawing = await context.Drawings.FindAsync(new object[] { command.DrawingId }, cancellationToken);
        if (drawing is null) throw new InvalidOperationException($"Drawing {command.DrawingId} not found.");

        var revisions = await context.DrawingRevisions
            .Where(revision => revision.DrawingId == command.DrawingId)
            .ToListAsync(cancellationToken);

        var revisionIds = revisions.Select(revision => revision.DrawingRevisionId).ToList();
        var issueRecords = await context.DrawingIssueRecords
            .Where(record => revisionIds.Contains(record.DrawingRevisionId))
            .ToListAsync(cancellationToken);

        var blobRefs = revisions
            .Select(revision => revision.BlobRef)
            .Where(blobRef => !string.IsNullOrEmpty(blobRef))
            .Cast<string>()
            .ToList();

        context.DrawingIssueRecords.RemoveRange(issueRecords);
        context.DrawingRevisions.RemoveRange(revisions);
        context.Drawings.Remove(drawing);
        await context.SaveChangesAsync(cancellationToken);

        foreach (var blobRef in blobRefs)
        {
            try { await blobStore.DeleteAsync(blobRef, cancellationToken); }
            catch { /* best effort — the rows are gone; an orphaned blob is harmless */ }
        }

        return new Acknowledgement(command.DrawingId);
    }
}
