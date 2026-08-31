using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.ClientPortal;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ClientPortal.Queries;

public sealed class ListMyClientVariationOrdersHandler
    : IQueryHandler<ListMyClientVariationOrders, IReadOnlyList<ClientPortalVariationOrder>>
{
    private readonly JpmsContext context;
    public ListMyClientVariationOrdersHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ClientPortalVariationOrder>> HandleAsync(
        ListMyClientVariationOrders query, CancellationToken cancellationToken)
    {
        var rows = await ClientProjects.VisibleVariationOrders(context)
            .Join(ClientProjects.For(context, query.ClientId),
                order => order.ProjectId, project => project.ProjectId,
                (order, project) => new { order, project.Name })
            .OrderByDescending(row => row.order.CreatedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.order.ToClientModel(row.Name)).ToList().AsReadOnly();
    }
}
