using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings.Queries;

public sealed class ListDrawingsForProjectHandler
    : IQueryHandler<ListDrawingsForProject, IReadOnlyList<Drawing>>
{
    private readonly JpmsContext context;

    public ListDrawingsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Drawing>> HandleAsync(ListDrawingsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.Drawings
            .Where(drawing => drawing.ProjectId == query.ProjectId)
            .OrderBy(drawing => drawing.DrawingCode)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
