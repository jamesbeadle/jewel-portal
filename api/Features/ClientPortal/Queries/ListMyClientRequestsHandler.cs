using Jewel.JPMS.Contracts.ClientPortal;

namespace Jewel.JPMS.Api.Features.ClientPortal.Queries;

public sealed class ListMyClientRequestsHandler
    : IQueryHandler<ListMyClientRequests, IReadOnlyList<ClientPortalRequest>>
{
    private readonly JpmsContext context;
    public ListMyClientRequestsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ClientPortalRequest>> HandleAsync(
        ListMyClientRequests query, CancellationToken cancellationToken)
    {
        var rows = await context.Requests
            .AsNoTracking()
            .Where(request => request.MergedIntoRequestId == null)
            .Join(ClientProjects.For(context, query.ClientId),
                request => request.ProjectId, project => project.ProjectId,
                (request, project) => new { request, project.Name })
            .OrderByDescending(row => row.request.RaisedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.request.ToClientModel(row.Name)).ToList().AsReadOnly();
    }
}
