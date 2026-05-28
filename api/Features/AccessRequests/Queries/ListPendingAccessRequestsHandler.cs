using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.AccessRequests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.AccessRequests.Queries;

public sealed class ListPendingAccessRequestsHandler
    : IQueryHandler<ListPendingAccessRequests, IReadOnlyList<AccessRequest>>
{
    private readonly JpmsContext context;

    public ListPendingAccessRequestsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<AccessRequest>> HandleAsync(
        ListPendingAccessRequests query, CancellationToken cancellationToken)
    {
        var entities = await context.AccessRequests
            .OrderByDescending(request => request.RequestedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
