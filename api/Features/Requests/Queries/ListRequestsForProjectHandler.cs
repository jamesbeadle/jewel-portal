using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListRequestsForProjectHandler : IQueryHandler<ListRequestsForProject, IReadOnlyList<Request>>
{
    private readonly JpmsContext context;
    public ListRequestsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Request>> HandleAsync(ListRequestsForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.Requests.Where(c => c.ProjectId == query.ProjectId).OrderByDescending(c => c.RaisedAt).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
