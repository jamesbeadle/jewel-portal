using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListRequestsForProjectHandler : IQueryHandler<ListRequestsForProject, IReadOnlyList<Request>>
{
    private readonly JpmsContext context;
    public ListRequestsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Request>> HandleAsync(ListRequestsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.Requests.AsNoTracking().Where(c => c.ProjectId == query.ProjectId).OrderByDescending(c => c.RaisedAt).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
