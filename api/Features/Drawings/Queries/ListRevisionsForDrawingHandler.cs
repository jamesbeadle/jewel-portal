using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings.Queries;

public sealed class ListRevisionsForDrawingHandler
    : IQueryHandler<ListRevisionsForDrawing, IReadOnlyList<DrawingRevision>>
{
    private readonly JpmsContext context;

    public ListRevisionsForDrawingHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<DrawingRevision>> HandleAsync(ListRevisionsForDrawing query, CancellationToken cancellationToken)
    {
        var entities = await context.DrawingRevisions
            .Where(revision => revision.DrawingId == query.DrawingId)
            .OrderByDescending(revision => revision.ReceivedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
