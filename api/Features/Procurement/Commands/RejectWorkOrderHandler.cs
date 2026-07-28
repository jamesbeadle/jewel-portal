using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// Rejects a draft work order — terminal. No number was ever minted (approval is what
/// mints one), so the sequence is untouched; the order simply stops counting anywhere.
/// Only drafts can be rejected: a live order that shouldn't proceed is cancelled, not
/// rejected, because it has already been issued to the supplier.
/// </summary>
public sealed class RejectWorkOrderHandler
    : ICommandHandler<RejectWorkOrder, WorkOrder>
{
    private readonly JpmsContext context;

    public RejectWorkOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<WorkOrder> HandleAsync(RejectWorkOrder command, CancellationToken cancellationToken)
    {
        var entity = await context.WorkOrders.FindAsync(new object[] { command.WorkOrderId }, cancellationToken);
        if (entity is null || !string.Equals(entity.ProjectId, command.ProjectId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This work order does not belong to this project.");
        if (entity.Status != (int)WorkOrderStatus.Draft)
            throw new InvalidOperationException("Only draft work orders can be rejected — this one has already been decided.");

        entity.Status = (int)WorkOrderStatus.Rejected;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
