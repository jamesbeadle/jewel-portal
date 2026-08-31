using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.ClientPortal;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ClientPortal.Queries;

public sealed class GetMyClientVariationOrderHandler
    : IQueryHandler<GetMyClientVariationOrder, ClientPortalVariationOrder?>
{
    private readonly JpmsContext context;
    public GetMyClientVariationOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<ClientPortalVariationOrder?> HandleAsync(
        GetMyClientVariationOrder query, CancellationToken cancellationToken)
    {
        var row = await ClientProjects.VisibleVariationOrders(context)
            .Where(order => order.VariationOrderId == query.VariationOrderId)
            .Join(ClientProjects.For(context, query.ClientId),
                order => order.ProjectId, project => project.ProjectId,
                (order, project) => new { order, project.Name })
            .FirstOrDefaultAsync(cancellationToken);
        return row?.order.ToClientModel(row.Name);
    }
}
