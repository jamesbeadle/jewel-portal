using Jewel.JPMS.Contracts.CostCenters;

namespace Jewel.JPMS.Api.Features.CostCenters.Queries;

public sealed class ListCostCentersHandler : IQueryHandler<ListCostCenters, IReadOnlyList<CostCenter>>
{
    private readonly JpmsContext context;
    public ListCostCentersHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<CostCenter>> HandleAsync(ListCostCenters query, CancellationToken cancellationToken)
    {
        var rows = context.CostCenters.AsNoTracking().AsQueryable();
        if (!query.IncludeInactive) rows = rows.Where(c => c.IsActive);
        var entities = await rows
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
