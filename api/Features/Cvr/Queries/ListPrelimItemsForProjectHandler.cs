using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Cvr.Queries;

public sealed class ListPrelimItemsForProjectHandler : IQueryHandler<ListPrelimItemsForProject, IReadOnlyList<PrelimItem>>
{
    private readonly JpmsContext context;
    public ListPrelimItemsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<PrelimItem>> HandleAsync(ListPrelimItemsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.PrelimItems.Where(p => p.ProjectId == query.ProjectId).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
