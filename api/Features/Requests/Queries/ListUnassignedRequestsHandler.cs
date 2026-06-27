using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

// Surfaces requests that aren't tied to a live project — a blank/null project id, or one that no
// longer matches any project row. These never appear in a project's register, so this is the only
// place a triager can find and recover them.
public sealed class ListUnassignedRequestsHandler : IQueryHandler<ListUnassignedRequests, IReadOnlyList<Request>>
{
    private readonly JpmsContext context;
    public ListUnassignedRequestsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Request>> HandleAsync(ListUnassignedRequests query, CancellationToken cancellationToken)
    {
        var projectIds = context.Projects.Select(p => p.ProjectId);
        var entities = await context.Requests
            .Where(r => r.ProjectId == null || r.ProjectId == "" || !projectIds.Contains(r.ProjectId))
            .OrderByDescending(r => r.RaisedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
