using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListWorkOrdersHandler
    : IQueryHandler<ListWorkOrders, IReadOnlyList<WorkOrder>>
{
    private readonly JpmsContext context;

    public ListWorkOrdersHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<WorkOrder>> HandleAsync(ListWorkOrders query, CancellationToken cancellationToken)
    {
        var entities = await context.WorkOrders.OrderByDescending(workOrder => workOrder.AwardedAt).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
