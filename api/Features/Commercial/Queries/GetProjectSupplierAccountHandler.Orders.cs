using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

public sealed partial class GetProjectSupplierAccountHandler
{
    // The account is what stands issued: Released and Complete orders. Drafts, rejected drafts and
    // cancelled orders are not commitments and can carry no links (the API refuses them).
    private async Task<List<WorkOrderEntity>> LiveOrdersAsync(
        GetProjectSupplierAccount query, CancellationToken cancellationToken) =>
        await context.WorkOrders.AsNoTracking()
            .Where(order => order.ProjectId == query.ProjectId
                            && order.SubcontractorId == query.SubcontractorId
                            && LiveStatuses.Contains(order.Status))
            .OrderBy(order => order.Number)
            .ToListAsync(cancellationToken);

    private async Task<Dictionary<string, List<WorkOrderLineEntity>>> LinesByOrderAsync(
        List<string> orderIds, CancellationToken cancellationToken)
    {
        if (orderIds.Count == 0) return new Dictionary<string, List<WorkOrderLineEntity>>(StringComparer.OrdinalIgnoreCase);
        var lines = await context.WorkOrderLines.AsNoTracking()
            .Where(line => orderIds.Contains(line.WorkOrderId))
            .OrderBy(line => line.SortOrder)
            .ToListAsync(cancellationToken);
        return lines
            .GroupBy(line => line.WorkOrderId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    private async Task<List<XeroLineWorkOrderLinkEntity>> LinksAsync(
        List<string> orderIds, CancellationToken cancellationToken)
    {
        if (orderIds.Count == 0) return new List<XeroLineWorkOrderLinkEntity>();
        return await context.XeroLineWorkOrderLinks.AsNoTracking()
            .Where(link => orderIds.Contains(link.WorkOrderId))
            .ToListAsync(cancellationToken);
    }

    // The order's figures are the WO Allocation tab's: invoiced is the signed sum of its link
    // slices; paid is Xero's settled part of those slices, else the migrated opening balance the
    // lines carry; the payment status says how far to trust the paid figure.
    private static ProjectSupplierAccountOrder BuildOrder(
        WorkOrderEntity order,
        string reference,
        IReadOnlyDictionary<string, List<WorkOrderLineEntity>> linesByOrder,
        IReadOnlyList<XeroLineWorkOrderLinkEntity> links,
        IReadOnlyList<ProjectSupplierAccountInvoice> invoices,
        IReadOnlyDictionary<string, decimal> paidByOrder)
    {
        var orderLines = linesByOrder.TryGetValue(order.WorkOrderId, out var found) ? found : new List<WorkOrderLineEntity>();
        var orderLinks = links
            .Where(link => string.Equals(link.WorkOrderId, order.WorkOrderId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var linkedLineCount = orderLinks
            .Select(link => link.XeroLedgerLineId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var invoiced = orderLinks.Sum(link => link.Amount);
        var openingBalance = orderLines.Sum(line => line.PaidToDate);
        var paid = paidByOrder.TryGetValue(order.WorkOrderId, out var fromXero) ? fromXero : openingBalance;

        return new ProjectSupplierAccountOrder(
            order.WorkOrderId,
            order.Number,
            reference,
            order.Title,
            (WorkOrderStatus)order.Status,
            order.AwardedAt,
            order.Value,
            orderLines.Select(line => new ProjectSupplierAccountOrderLine(line.Title, line.CostCode, line.LineTotal)).ToList(),
            invoiced,
            paid,
            WorkOrderPaymentStatuses.For(linkedLineCount, paid, order.Value),
            InvoiceSharesOf(order, invoices));
    }

    // Read off the invoices already built, so the two halves of the account share one figure.
    private static IReadOnlyList<ProjectSupplierAccountInvoiceShare> InvoiceSharesOf(
        WorkOrderEntity order, IReadOnlyList<ProjectSupplierAccountInvoice> invoices) =>
        invoices
            .SelectMany(invoice => invoice.Orders
                .Where(share => string.Equals(share.WorkOrderId, order.WorkOrderId, StringComparison.OrdinalIgnoreCase))
                .Select(share => new ProjectSupplierAccountInvoiceShare(invoice.XeroInvoiceId, invoice.InvoiceNumber, share.Amount)))
            .ToList();
}
