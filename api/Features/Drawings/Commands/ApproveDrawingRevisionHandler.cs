using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

/// <summary>
/// Approves a revision. The target becomes the single Approved (latest) revision of its drawing;
/// every other revision of that drawing is archived; the drawing's current approved label is set.
/// Invariant afterwards: exactly one Approved revision exists for the drawing.
/// </summary>
public sealed class ApproveDrawingRevisionHandler
    : ICommandHandler<ApproveDrawingRevision, DrawingRevision>
{
    private readonly JpmsContext context;

    public ApproveDrawingRevisionHandler(JpmsContext context) { this.context = context; }

    public async Task<DrawingRevision> HandleAsync(ApproveDrawingRevision command, CancellationToken cancellationToken)
    {
        var drawing = await context.Drawings.FindAsync(new object[] { command.DrawingId }, cancellationToken);
        if (drawing is null) throw new InvalidOperationException($"Drawing {command.DrawingId} not found.");

        var revisions = await context.DrawingRevisions
            .Where(revision => revision.DrawingId == command.DrawingId)
            .ToListAsync(cancellationToken);

        var target = revisions.FirstOrDefault(revision => revision.DrawingRevisionId == command.DrawingRevisionId);
        if (target is null)
            throw new InvalidOperationException($"Revision {command.DrawingRevisionId} not found on drawing {command.DrawingId}.");

        var now = DateTimeOffset.UtcNow;

        // Archive every other revision (previous approved + any lingering unapproved) so exactly one stays live.
        foreach (var revision in revisions)
        {
            if (revision.DrawingRevisionId == target.DrawingRevisionId) continue;
            if ((DrawingApprovalStatus)revision.ApprovalStatus == DrawingApprovalStatus.Archived) continue;
            revision.ApprovalStatus = (int)DrawingApprovalStatus.Archived;
            revision.SupersededAt = now;
        }

        target.ApprovalStatus = (int)DrawingApprovalStatus.Approved;
        target.ApprovedAt = now;
        target.ApprovedByEmail = command.ApprovedByEmail;
        target.SupersededAt = null;

        drawing.CurrentApprovedRevisionLabel = target.RevisionLabel;

        await context.SaveChangesAsync(cancellationToken);
        return target.ToModel();
    }
}
