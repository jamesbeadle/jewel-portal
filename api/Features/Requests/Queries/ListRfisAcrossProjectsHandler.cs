using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

// Every RFI on every live project, for the cross-project RFI dashboard. Requests whose project id
// no longer matches a project row are excluded — those are stranded and surface via the
// unassigned-requests triage view instead.
public sealed class ListRfisAcrossProjectsHandler : IQueryHandler<ListRfisAcrossProjects, IReadOnlyList<Request>>
{
    private readonly JpmsContext context;
    public ListRfisAcrossProjectsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Request>> HandleAsync(ListRfisAcrossProjects query, CancellationToken cancellationToken)
    {
        var projectIds = context.Projects.Select(p => p.ProjectId);
        var entities = await context.Requests
            .Where(r => r.Kind == (int)RequestType.Rfi && projectIds.Contains(r.ProjectId))
            .OrderByDescending(r => r.RaisedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
