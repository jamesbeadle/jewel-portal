using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

/// <summary>
/// Sets a revision's label after upload. If that revision is the drawing's Approved one, the
/// drawing's current approved label follows it, so the register's "Latest approved" stays true.
/// </summary>
public sealed class SetDrawingRevisionLabelHandler
    : ICommandHandler<SetDrawingRevisionLabel, DrawingRevision>
{
    private readonly JpmsContext context;

    public SetDrawingRevisionLabelHandler(JpmsContext context) { this.context = context; }

    public async Task<DrawingRevision> HandleAsync(SetDrawingRevisionLabel command, CancellationToken cancellationToken)
    {
        var drawing = await context.Drawings.FindAsync(new object[] { command.DrawingId }, cancellationToken);
        if (drawing is null) throw new InvalidOperationException($"Drawing {command.DrawingId} not found.");

        var revision = await context.DrawingRevisions.FindAsync(new object[] { command.DrawingRevisionId }, cancellationToken);
        if (revision is null || revision.DrawingId != drawing.DrawingId)
            throw new InvalidOperationException($"Revision {command.DrawingRevisionId} not found on drawing {command.DrawingId}.");

        var label = (command.RevisionLabel ?? "").Trim();
        revision.RevisionLabel = label;
        // Giving a revision its label is exactly what resolves an ambiguous upload.
        revision.IsAmbiguous = false;

        var isTheApprovedRevision = (DrawingApprovalStatus)revision.ApprovalStatus == DrawingApprovalStatus.Approved;
        if (isTheApprovedRevision) drawing.CurrentApprovedRevisionLabel = label;

        await context.SaveChangesAsync(cancellationToken);
        return revision.ToModel();
    }
}
