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
        var orders = await context.WorkOrders.AsNoTracking()
            .Where(order => order.ProjectId == query.ProjectId)
            .OrderBy(order => order.Number)
            .ToListAsync(cancellationToken);
        if (orders.Count == 0) return Array.Empty<WorkOrderInvoiceSummary>();

        // Signed sums of the link slices paying each order — a bill split across several
        // orders contributes each slice to its own order. Slices only exist on whole-line
        // allocations to this project by construction: the link command enforces it, and
        // re-allocating a line off the project clears its links.
        var linkedTotals = await context.XeroLineWorkOrderLinks.AsNoTracking()
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
        var namesById = await context.Subcontractors.AsNoTracking()
            .Where(sub => subcontractorIds.Contains(sub.SubcontractorId))
            .ToDictionaryAsync(sub => sub.SubcontractorId, sub => sub.CompanyName, cancellationToken);

        // Paid, from the settled bills linked to each order — and the Buildertrend opening
        // balance behind it, which is all there is to go on for an order nothing is linked to.
        var paidByOrder = await WorkOrderPaidPositions.ForProjectAsync(context, query.ProjectId, cancellationToken);

        var orderIds = orders.Select(order => order.WorkOrderId).ToList();
        var openingByOrder = (await context.WorkOrderLines.AsNoTracking()
                .Where(line => orderIds.Contains(line.WorkOrderId))
                .GroupBy(line => line.WorkOrderId)
                .Select(group => new { WorkOrderId = group.Key, Opening = group.Sum(line => line.PaidToDate) })
                .ToListAsync(cancellationToken))
            .ToDictionary(row => row.WorkOrderId, row => row.Opening, StringComparer.OrdinalIgnoreCase);

        // When JPMS last heard from Xero at all. The sync is a button on the allocation page,
        // not a schedule, so a paid figure is only ever as current as this — the tab says so
        // rather than letting a stale zero pass for a confirmed one.
        var ledgerSyncedAtUtc = await context.XeroLedgerLines.AsNoTracking()
            .MaxAsync(line => (DateTimeOffset?)line.LastSyncedAtUtc, cancellationToken);

        return orders.Select(order =>
            {
                totalsByOrder.TryGetValue(order.WorkOrderId, out var linked);
                var invoiced = linked?.Invoiced ?? 0m;
                var remaining = order.Value - invoiced;
                var invoicingStatus = invoiced == 0m ? WorkOrderInvoicingStatus.NotInvoiced
                    : remaining < 0m ? WorkOrderInvoicingStatus.OverInvoiced
                    : remaining == 0m ? WorkOrderInvoicingStatus.FullyInvoiced
                    : WorkOrderInvoicingStatus.PartInvoiced;

                var linkedLineCount = linked?.Count ?? 0;
                openingByOrder.TryGetValue(order.WorkOrderId, out var opening);
                var paid = paidByOrder.TryGetValue(order.WorkOrderId, out var fromXero) ? fromXero : opening;

                return new WorkOrderInvoiceSummary(
                    order.WorkOrderId,
                    order.Number,
                    order.Title,
                    namesById.TryGetValue(order.SubcontractorId, out var name) ? name : "(unknown supplier)",
                    (WorkOrderStatus)order.Status,
                    order.Value,
                    invoiced,
                    remaining,
                    linkedLineCount,
                    invoicingStatus,
                    paid,
                    WorkOrderPaymentStatuses.For(linkedLineCount, paid, order.Value),
                    ledgerSyncedAtUtc);
            })
            .ToList();
    }
}
