using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.CostCenters;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.CostCenters.Queries;

public sealed class ListCostCentersHandler : IQueryHandler<ListCostCenters, IReadOnlyList<CostCenter>>
{
    private readonly JpmsContext context;
    public ListCostCentersHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<CostCenter>> HandleAsync(ListCostCenters query, CancellationToken cancellationToken)
    {
        var rows = context.CostCenters.AsQueryable();
        if (!query.IncludeInactive) rows = rows.Where(c => c.IsActive);
        var entities = await rows
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
