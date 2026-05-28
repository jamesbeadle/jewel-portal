using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class IssueDrawingRevisionHandler
    : ICommandHandler<IssueDrawingRevision, DrawingRevision>
{
    private readonly JpmsContext context;

    public IssueDrawingRevisionHandler(JpmsContext context) { this.context = context; }

    public async Task<DrawingRevision> HandleAsync(IssueDrawingRevision command, CancellationToken cancellationToken)
    {
        var drawing = await context.Drawings.FindAsync(new object[] { command.DrawingId }, cancellationToken);
        if (drawing is null) throw new InvalidOperationException($"Drawing {command.DrawingId} not found.");

        var supersededAt = DateTimeOffset.UtcNow;
        var priorActiveRevisions = await context.DrawingRevisions
            .Where(revision => revision.DrawingId == command.DrawingId && revision.SupersededAt == null)
            .ToListAsync(cancellationToken);
        foreach (var prior in priorActiveRevisions) prior.SupersededAt = supersededAt;

        var newRevision = new DrawingRevisionEntity
        {
            DrawingRevisionId = DrawingIdentifierFactory.NextDrawingRevisionId(),
            DrawingId = command.DrawingId,
            RevisionLabel = command.RevisionLabel,
            FileName = command.FileName,
            IssuedByEmail = command.IssuedByEmail,
            ReceivedAt = supersededAt,
            SupersededAt = null,
            IsAmbiguous = false,
            ViewCount = 0
        };
        context.DrawingRevisions.Add(newRevision);
        drawing.CurrentRevision = command.RevisionLabel;

        await context.SaveChangesAsync(cancellationToken);
        return newRevision.ToModel();
    }
}
