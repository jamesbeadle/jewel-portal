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
        var entities = await context.CostCenters
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
