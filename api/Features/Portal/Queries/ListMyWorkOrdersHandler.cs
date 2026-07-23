using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Procurement;
using Jewel.JPMS.Contracts.Portal;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Portal.Queries;

/// <summary>
/// The subcontractor's own work orders, newest first. Drafts are excluded — a work order only
/// exists for the supplier once it has been Released (issued). Complete and Cancelled orders stay
/// visible as history.
/// </summary>
public sealed class ListMyWorkOrdersHandler : IQueryHandler<ListMyWorkOrders, IReadOnlyList<PortalWorkOrder>>
{
    private readonly JpmsContext context;

    public ListMyWorkOrdersHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<PortalWorkOrder>> HandleAsync(ListMyWorkOrders query, CancellationToken cancellationToken)
    {
        var orders = await context.WorkOrders
            .Where(order => order.SubcontractorId == query.SubcontractorId
                && order.Status != (int)WorkOrderStatus.Draft)
            .OrderByDescending(order => order.AwardedAt)
            .ToListAsync(cancellationToken);
        if (orders.Count == 0) return Array.Empty<PortalWorkOrder>();

        var orderIds = orders.Select(order => order.WorkOrderId).ToList();
        var linesByOrder = (await context.WorkOrderLines
                .Where(line => orderIds.Contains(line.WorkOrderId))
                .ToListAsync(cancellationToken))
            .GroupBy(line => line.WorkOrderId)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var projectIds = orders.Select(order => order.ProjectId).Distinct().ToList();
        var projectsById = (await context.Projects
                .Where(project => projectIds.Contains(project.ProjectId))
                .ToListAsync(cancellationToken))
            .ToDictionary(project => project.ProjectId, StringComparer.OrdinalIgnoreCase);

        // Approver display names for the printed PO's signature block — the portal can't read the
        // user directory, so the join happens here. Falls back to the raw email client-side.
        var approverEmails = orders.Select(order => order.AwardedByEmail)
            .Where(email => !string.IsNullOrWhiteSpace(email)).Distinct().ToList();
        var approverNamesByEmail = (await context.DirectoryUsers
                .Where(user => approverEmails.Contains(user.Email))
                .ToListAsync(cancellationToken))
            .ToDictionary(user => user.Email, user => user.DisplayName, StringComparer.OrdinalIgnoreCase);

        return orders.Select(order =>
            {
                var project = projectsById.TryGetValue(order.ProjectId, out var found) ? found : null;
                return new PortalWorkOrder(
                    order.ToModel(),
                    project?.Name ?? "(project)",
                    linesByOrder.TryGetValue(order.WorkOrderId, out var lines)
                        ? lines.OrderBy(line => line.SortOrder).Select(line => line.ToModel()).ToList()
                        : new List<WorkOrderLine>(),
                    approverNamesByEmail.TryGetValue(order.AwardedByEmail, out var approver) ? approver : "",
                    project is null
                        ? ""
                        : string.Join(", ", new[] { project.AddressLine, project.Town, project.Postcode }
                            .Where(part => !string.IsNullOrWhiteSpace(part))));
            })
            .ToList()
            .AsReadOnly();
    }
}
