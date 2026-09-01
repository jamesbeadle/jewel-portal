using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListProjectWorkOrdersHandler
    : IQueryHandler<ListProjectWorkOrders, IReadOnlyList<ProjectWorkOrderDetail>>
{
    private readonly JpmsContext context;

    public ListProjectWorkOrdersHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ProjectWorkOrderDetail>> HandleAsync(ListProjectWorkOrders query, CancellationToken cancellationToken)
    {
        var orders = await context.WorkOrders.AsNoTracking()
            .Where(order => order.ProjectId == query.ProjectId)
            // Drafts all sit at Number 0, so they need a stable tiebreak of their own —
            // creation order, then id, matching the statement handler's convention.
            .OrderBy(order => order.Number)
            .ThenBy(order => order.CreatedAt)
            .ThenBy(order => order.WorkOrderId)
            .ToListAsync(cancellationToken);
        if (orders.Count == 0) return Array.Empty<ProjectWorkOrderDetail>();

        var orderIds = orders.Select(order => order.WorkOrderId).ToList();
        var linesByOrder = (await context.WorkOrderLines.AsNoTracking()
                .Where(line => orderIds.Contains(line.WorkOrderId))
                .ToListAsync(cancellationToken))
            .GroupBy(line => line.WorkOrderId)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var subcontractorIds = orders.Select(order => order.SubcontractorId).Distinct().ToList();
        var namesById = await context.Subcontractors.AsNoTracking()
            .Where(sub => subcontractorIds.Contains(sub.SubcontractorId))
            .ToDictionaryAsync(sub => sub.SubcontractorId, sub => sub.CompanyName, cancellationToken);

        // What each order has actually been paid, from the settled Xero bills linked to it. The
        // stored per-line PaidToDate is only the Buildertrend opening balance and stands in for
        // orders the ledger has nothing linked against — see WorkOrderPaidPositions for why the
        // two are never added together. Every consumer of this query (the Work Orders tab and its
        // export, the printed purchase order, the cost-centre drill-down) reads the line figure,
        // so restating it here is what makes them all agree with Xero.
        var paidByOrder = await WorkOrderPaidPositions.ForProjectAsync(context, query.ProjectId, cancellationToken);

        return orders.Select(order =>
            {
                var lines = linesByOrder.TryGetValue(order.WorkOrderId, out var stored)
                    ? stored.OrderBy(line => line.SortOrder).Select(line => line.ToModel()).ToList()
                    : new List<WorkOrderLine>();

                return new ProjectWorkOrderDetail(
                    order.ToModel(),
                    namesById.TryGetValue(order.SubcontractorId, out var name) ? name : "(unknown supplier)",
                    paidByOrder.TryGetValue(order.WorkOrderId, out var paid)
                        ? SpreadPaidAcrossLines(lines, paid)
                        : lines);
            })
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Restates each line's PaidToDate as its share of the order's paid position. Links are held
    /// per ORDER and the Work Orders tab totals per LINE, so the amount is spread pro-rata by line
    /// value with the last line absorbing the rounding remainder — the same penny-safe split the
    /// re-code uses, so the lines always sum back to the order's paid position exactly. Where the
    /// lines carry no value between them there is nothing to weight by, and the whole amount sits
    /// on the first line rather than vanishing out of the tab's totals.
    /// Internal because WorkOrderPoDocumentBuilder restates the same figures for the emailed
    /// purchase-order PDF — one implementation, so the PDF's Paid column always agrees with the tab.
    /// </summary>
    internal static List<WorkOrderLine> SpreadPaidAcrossLines(List<WorkOrderLine> lines, decimal paid)
    {
        if (lines.Count == 0) return lines;

        var weights = lines.Select(line => line.LineTotal).ToList();
        if (weights.Sum() == 0m)
            return lines.Select((line, index) => line with { PaidToDate = index == 0 ? paid : 0m }).ToList();

        var shares = XeroSplitMaths.ProportionalShares(paid, weights);
        return lines.Select((line, index) => line with { PaidToDate = shares[index] }).ToList();
    }
}
