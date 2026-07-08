using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListProjectWorkOrdersHandler
    : IQueryHandler<ListProjectWorkOrders, IReadOnlyList<ProjectWorkOrderDetail>>
{
    private readonly JpmsContext context;

    public ListProjectWorkOrdersHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ProjectWorkOrderDetail>> HandleAsync(ListProjectWorkOrders query, CancellationToken cancellationToken)
    {
        var orders = await context.WorkOrders
            .Where(order => order.ProjectId == query.ProjectId)
            .OrderBy(order => order.Number)
            .ToListAsync(cancellationToken);
        if (orders.Count == 0) return Array.Empty<ProjectWorkOrderDetail>();

        var orderIds = orders.Select(order => order.WorkOrderId).ToList();
        var linesByOrder = (await context.WorkOrderLines
                .Where(line => orderIds.Contains(line.WorkOrderId))
                .ToListAsync(cancellationToken))
            .GroupBy(line => line.WorkOrderId)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var subcontractorIds = orders.Select(order => order.SubcontractorId).Distinct().ToList();
        var namesById = await context.Subcontractors
            .Where(sub => subcontractorIds.Contains(sub.SubcontractorId))
            .ToDictionaryAsync(sub => sub.SubcontractorId, sub => sub.CompanyName, cancellationToken);

        return orders.Select(order => new ProjectWorkOrderDetail(
                order.ToModel(),
                namesById.TryGetValue(order.SubcontractorId, out var name) ? name : "(unknown supplier)",
                linesByOrder.TryGetValue(order.WorkOrderId, out var lines)
                    ? lines.OrderBy(line => line.SortOrder).Select(line => line.ToModel()).ToList()
                    : new List<WorkOrderLine>()))
            .ToList()
            .AsReadOnly();
    }
}
