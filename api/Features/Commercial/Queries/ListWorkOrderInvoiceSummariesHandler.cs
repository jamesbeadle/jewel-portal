using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed class ListWorkOrderInvoiceSummariesHandler
    : IQueryHandler<ListWorkOrderInvoiceSummaries, IReadOnlyList<WorkOrderInvoiceSummary>>
{
    private readonly JpmsContext context;

    public ListWorkOrderInvoiceSummariesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<WorkOrderInvoiceSummary>> HandleAsync(
        ListWorkOrderInvoiceSummaries query, CancellationToken cancellationToken)
    {
        var orders = await context.WorkOrders
            .Where(order => order.ProjectId == query.ProjectId)
            .OrderBy(order => order.Number)
            .ToListAsync(cancellationToken);
        if (orders.Count == 0) return Array.Empty<WorkOrderInvoiceSummary>();

        // Signed sums of the link slices paying each order — a bill split across several
        // orders contributes each slice to its own order. Slices only exist on whole-line
        // allocations to this project by construction: the link command enforces it, and
        // re-allocating a line off the project clears its links.
        var linkedTotals = await context.XeroLineWorkOrderLinks
            .Where(link => link.ProjectId == query.ProjectId)
            .GroupBy(link => link.WorkOrderId)
            .Select(group => new
            {
                WorkOrderId = group.Key,
                Invoiced = group.Sum(link => link.Amount),
                Count = group.Select(link => link.XeroLedgerLineId).Distinct().Count()
            })
            .ToListAsync(cancellationToken);
        var totalsByOrder = linkedTotals.ToDictionary(total => total.WorkOrderId, StringComparer.OrdinalIgnoreCase);

        var subcontractorIds = orders.Select(order => order.SubcontractorId).Distinct().ToList();
        var namesById = await context.Subcontractors
            .Where(sub => subcontractorIds.Contains(sub.SubcontractorId))
            .ToDictionaryAsync(sub => sub.SubcontractorId, sub => sub.CompanyName, cancellationToken);

        return orders.Select(order =>
            {
                totalsByOrder.TryGetValue(order.WorkOrderId, out var linked);
                var invoiced = linked?.Invoiced ?? 0m;
                var remaining = order.Value - invoiced;
                var invoicingStatus = invoiced == 0m ? WorkOrderInvoicingStatus.NotInvoiced
                    : remaining < 0m ? WorkOrderInvoicingStatus.OverInvoiced
                    : remaining == 0m ? WorkOrderInvoicingStatus.FullyInvoiced
                    : WorkOrderInvoicingStatus.PartInvoiced;
                return new WorkOrderInvoiceSummary(
                    order.WorkOrderId,
                    order.Number,
                    order.Title,
                    namesById.TryGetValue(order.SubcontractorId, out var name) ? name : "(unknown supplier)",
                    (WorkOrderStatus)order.Status,
                    order.Value,
                    invoiced,
                    remaining,
                    linked?.Count ?? 0,
                    invoicingStatus);
            })
            .ToList();
    }
}
