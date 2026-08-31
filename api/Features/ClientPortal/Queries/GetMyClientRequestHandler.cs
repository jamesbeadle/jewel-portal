using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.ClientPortal;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ClientPortal.Queries;

public sealed class GetMyClientRequestHandler : IQueryHandler<GetMyClientRequest, ClientPortalRequest?>
{
    private readonly JpmsContext context;
    public GetMyClientRequestHandler(JpmsContext context) { this.context = context; }

    public async Task<ClientPortalRequest?> HandleAsync(
        GetMyClientRequest query, CancellationToken cancellationToken)
    {
        var row = await context.Requests
            .AsNoTracking()
            .Where(request => request.RequestId == query.RequestId && request.MergedIntoRequestId == null)
            .Join(ClientProjects.For(context, query.ClientId),
                request => request.ProjectId, project => project.ProjectId,
                (request, project) => new { request, project.Name })
            .FirstOrDefaultAsync(cancellationToken);
        return row?.request.ToClientModel(row.Name);
    }
}
