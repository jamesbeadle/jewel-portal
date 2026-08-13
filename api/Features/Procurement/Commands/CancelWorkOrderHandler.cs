using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// Cancels a released work order — terminal. The order keeps its minted number (a purchase
/// order went out under that reference, so the record is voided, never deleted) but stops
/// counting everywhere: issued totals, the Financials tab's committed figures, WO allocation
/// and the supplier portal all read Cancelled as "not a commitment any more". Only a live
/// order can be cancelled — drafts are rejected instead (RejectWorkOrder), and an order with
/// bills linked or money recorded against it is refused outright rather than quietly
/// orphaning the costs: unlink or re-code its bills on the WO Allocation tab first.
/// </summary>
public sealed class CancelWorkOrderHandler
    : ICommandHandler<CancelWorkOrder, WorkOrder>
{
    private readonly JpmsContext context;

    public CancelWorkOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<WorkOrder> HandleAsync(CancelWorkOrder command, CancellationToken cancellationToken)
    {
        var entity = await context.WorkOrders.FindAsync(new object[] { command.WorkOrderId }, cancellationToken);
        if (entity is null || !string.Equals(entity.ProjectId, command.ProjectId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This work order does not belong to this project.");
        if (entity.Status == (int)WorkOrderStatus.Draft)
            throw new InvalidOperationException("This order is still a draft — reject it instead; cancelling is for orders already issued.");
        if (entity.Status != (int)WorkOrderStatus.Released)
            throw new InvalidOperationException("Only a released work order can be cancelled — this one has already been closed.");

        // Money guards: cancelling voids the commitment, so nothing may already be recorded
        // against it. A linked bill or a paid balance means real costs would be orphaned —
        // the caller re-codes or unlinks those on the WO Allocation tab first.
        var hasLinkedBills = await context.XeroLineWorkOrderLinks.AsNoTracking()
            .AnyAsync(link => link.WorkOrderId == command.WorkOrderId, cancellationToken);
        if (hasLinkedBills)
            throw new InvalidOperationException(
                "Bills are linked to this order on the WO Allocation tab. Unlink or re-allocate them first, then cancel.");

        var paidToDate = await context.WorkOrderLines.AsNoTracking()
            .Where(line => line.WorkOrderId == command.WorkOrderId)
            .SumAsync(line => (decimal?)line.PaidToDate, cancellationToken) ?? 0m;
        if (paidToDate != 0m)
            throw new InvalidOperationException(
                "This order has a paid balance recorded against it, so it can't be cancelled — the money has already moved.");

        entity.Status = (int)WorkOrderStatus.Cancelled;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
