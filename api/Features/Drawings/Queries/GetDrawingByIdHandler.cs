using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Queries;

public sealed class GetDrawingByIdHandler : IQueryHandler<GetDrawingById, Drawing?>
{
    private readonly JpmsContext context;

    public GetDrawingByIdHandler(JpmsContext context) { this.context = context; }

    public async Task<Drawing?> HandleAsync(GetDrawingById query, CancellationToken cancellationToken)
    {
        var entity = await context.Drawings.AsNoTracking()
            .FirstOrDefaultAsync(drawing => drawing.DrawingId == query.DrawingId, cancellationToken);
        if (entity is null) return null;

        // Same rollup as the register, so a single drawing reads identically to its register row.
        var summaries = await context.DrawingRevisions.AsNoTracking()
            .Where(revision => revision.DrawingId == query.DrawingId)
            .Select(revision => new DrawingRevisionRollup.RevisionSummary(
                revision.DrawingId, revision.ApprovalStatus, revision.ReceivedAt, revision.FileName,
                revision.MetadataExtractedAt, revision.AnalysedAt))
            .ToListAsync(cancellationToken);
        return entity.ToModel(DrawingRevisionRollup.Of(summaries));
    }
}
