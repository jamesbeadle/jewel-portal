using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Cvr.Queries;

public sealed class ListForecastComponentsForProjectHandler : IQueryHandler<ListForecastComponentsForProject, IReadOnlyList<ForecastComponent>>
{
    private readonly JpmsContext context;
    public ListForecastComponentsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ForecastComponent>> HandleAsync(ListForecastComponentsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.ForecastComponents.Where(f => f.ProjectId == query.ProjectId).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
