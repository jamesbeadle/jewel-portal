using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Audit;
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
    private readonly AuditTrail audit;

    public RejectWorkOrderHandler(JpmsContext context, AuditTrail audit)
    {
        this.context = context;
        this.audit = audit;
    }

    public async Task<WorkOrder> HandleAsync(RejectWorkOrder command, CancellationToken cancellationToken)
    {
        var entity = await context.WorkOrders.FindAsync(new object[] { command.WorkOrderId }, cancellationToken);
        if (entity is null || !string.Equals(entity.ProjectId, command.ProjectId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This work order does not belong to this project.");
        if (entity.Status != (int)WorkOrderStatus.Draft)
            throw new InvalidOperationException("Only draft work orders can be rejected — this one has already been decided.");

        entity.Status = (int)WorkOrderStatus.Rejected;

        await context.SaveChangesAsync(cancellationToken);

        // The entity stamps nothing at rejection (no date, no decider), so this best-effort row
        // is the only dated record of the decision on the order's timeline.
        await audit.WriteAsync(
            AuditEventType.WorkOrderRejected,
            "Draft rejected — never issued; it counts nowhere.",
            projectId: entity.ProjectId,
            recordType: RecordType.WorkOrder,
            recordId: entity.WorkOrderId,
            recordReference: entity.Reference,
            cancellationToken: cancellationToken);

        return entity.ToModel();
    }
}
