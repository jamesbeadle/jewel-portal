using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class ListVariationOrdersForProjectHandler : IQueryHandler<ListVariationOrdersForProject, IReadOnlyList<VariationOrder>>
{
    private readonly JpmsContext context;
    public ListVariationOrdersForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<VariationOrder>> HandleAsync(ListVariationOrdersForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.VariationOrders
            .Where(vo => vo.ProjectId == query.ProjectId)
            .OrderByDescending(vo => vo.Number)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
