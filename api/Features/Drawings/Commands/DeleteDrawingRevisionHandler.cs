using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

/// <summary>
/// Hard-deletes a single revision, plus any issue records that referenced it. If the deleted
/// revision was the drawing's approved one, the drawing reverts to "no approved revision" (archived
/// revisions are left as-is — approval history is not rewritten). The database rows are removed
/// first; blob cleanup runs afterwards as best effort.
/// </summary>
public sealed class DeleteDrawingRevisionHandler : ICommandHandler<DeleteDrawingRevision, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly IDrawingBlobStore blobStore;

    public DeleteDrawingRevisionHandler(JpmsContext context, IDrawingBlobStore blobStore)
    {
        this.context = context;
        this.blobStore = blobStore;
    }

    public async Task<Acknowledgement> HandleAsync(DeleteDrawingRevision command, CancellationToken cancellationToken)
    {
        var revision = await context.DrawingRevisions.FindAsync(new object[] { command.DrawingRevisionId }, cancellationToken);
        if (revision is null || revision.DrawingId != command.DrawingId)
            throw new InvalidOperationException($"Revision {command.DrawingRevisionId} not found on drawing {command.DrawingId}.");

        var issueRecords = await context.DrawingIssueRecords
            .Where(record => record.DrawingRevisionId == command.DrawingRevisionId)
            .ToListAsync(cancellationToken);

        // Deleting the approved revision leaves the drawing with none approved (the approve flow
        // archives every other revision, so there is no runner-up to promote).
        if ((DrawingApprovalStatus)revision.ApprovalStatus == DrawingApprovalStatus.Approved)
        {
            var drawing = await context.Drawings.FindAsync(new object[] { command.DrawingId }, cancellationToken);
            if (drawing is not null) drawing.CurrentApprovedRevisionLabel = null;
        }

        var blobRef = revision.BlobRef;

        context.DrawingIssueRecords.RemoveRange(issueRecords);
        context.DrawingRevisions.Remove(revision);
        await context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(blobRef))
        {
            try { await blobStore.DeleteAsync(blobRef, cancellationToken); }
            catch { /* best effort — the row is gone; an orphaned blob is harmless */ }
        }

        return new Acknowledgement(command.DrawingRevisionId);
    }
}
