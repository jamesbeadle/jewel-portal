using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Cvr.Queries;

public sealed class ListEotsForProjectHandler : IQueryHandler<ListEotsForProject, IReadOnlyList<Eot>>
{
    private readonly JpmsContext context;
    public ListEotsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Eot>> HandleAsync(ListEotsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.Eots.Where(e => e.ProjectId == query.ProjectId).OrderByDescending(e => e.GrantedAt).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
