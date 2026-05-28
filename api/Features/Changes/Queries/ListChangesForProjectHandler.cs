using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Changes;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Changes.Queries;

public sealed class ListChangesForProjectHandler : IQueryHandler<ListChangesForProject, IReadOnlyList<ChangeRecord>>
{
    private readonly JpmsContext context;
    public ListChangesForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ChangeRecord>> HandleAsync(ListChangesForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.ChangeRecords.Where(c => c.ProjectId == query.ProjectId).OrderByDescending(c => c.RaisedAt).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
