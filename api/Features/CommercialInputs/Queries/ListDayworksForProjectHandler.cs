using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Queries;

public sealed class ListDayworksForProjectHandler
    : IQueryHandler<ListDayworksForProject, IReadOnlyList<Daywork>>
{
    private readonly JpmsContext context;

    public ListDayworksForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Daywork>> HandleAsync(
        ListDayworksForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.Dayworks
            .Where(daywork => daywork.ProjectId == query.ProjectId)
            .OrderByDescending(daywork => daywork.WorkedOn)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
