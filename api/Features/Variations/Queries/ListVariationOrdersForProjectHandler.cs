using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Queries;

public sealed class ListVariationOrdersForProjectHandler : IQueryHandler<ListVariationOrdersForProject, IReadOnlyList<VariationOrder>>
{
    private readonly JpmsContext context;
    public ListVariationOrdersForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<VariationOrder>> HandleAsync(ListVariationOrdersForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.VariationOrders.AsNoTracking()
            .Where(vo => vo.ProjectId == query.ProjectId)
            .OrderByDescending(vo => vo.Number)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
