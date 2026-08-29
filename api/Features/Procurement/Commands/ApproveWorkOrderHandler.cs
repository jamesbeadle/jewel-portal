using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// Approves a draft work order: mints the next sequential per-project number (the same
/// counter awarded / variation / manual orders draw from, so paperwork cross-references
/// hold) and moves the order to Released. AwardedAt/AwardedByEmail are stamped at
/// approval — that is the moment the order is actually issued; CreatedAt keeps the
/// drafting time. Only drafts can be approved; everything else already has a number.
/// </summary>
public sealed class ApproveWorkOrderHandler
    : ICommandHandler<ApproveWorkOrder, WorkOrder>
{
    private readonly JpmsContext context;
    private readonly AuditTrail audit;

    public ApproveWorkOrderHandler(JpmsContext context, AuditTrail audit)
    {
        this.context = context;
        this.audit = audit;
    }

    public async Task<WorkOrder> HandleAsync(ApproveWorkOrder command, CancellationToken cancellationToken)
    {
        var entity = await context.WorkOrders.FindAsync(new object[] { command.WorkOrderId }, cancellationToken);
        if (entity is null || !string.Equals(entity.ProjectId, command.ProjectId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This work order does not belong to this project.");
        if (entity.Status == (int)WorkOrderStatus.Rejected)
            throw new InvalidOperationException("This work order was rejected — raise a fresh order instead.");
        if (entity.Status != (int)WorkOrderStatus.Draft)
            throw new InvalidOperationException("Only draft work orders can be approved — this one already has been.");

        // Same mint as CreateManualWorkOrder / AwardBidPackage / IssueWorkOrderForVariationOrder.
        // Drafts all sit at 0, so they never win the MAX. A draft that already carries a number
        // (legacy seeded data keeps its Buildertrend PO number) keeps it — renumbering would
        // break paperwork cross-references and detach any mail tagged against its reference.
        var nextNumber = entity.Number > 0
            ? entity.Number
            : (await context.WorkOrders
                .Where(order => order.ProjectId == command.ProjectId)
                .MaxAsync(order => (int?)order.Number, cancellationToken) ?? 0) + 1;

        entity.Number = nextNumber;
        entity.Status = (int)WorkOrderStatus.Released;
        entity.AwardedAt = DateTimeOffset.UtcNow;
        entity.AwardedByEmail = command.ApprovedByEmail;

        await context.SaveChangesAsync(cancellationToken);

        // After the save, like every AuditTrail caller — best-effort, never fails the approval.
        // The entity stamps (AwardedAt/AwardedByEmail) are the primary record; this row puts the
        // decision on the order's timeline alongside its sends and filed correspondence.
        await audit.WriteAsync(
            AuditEventType.WorkOrderApproved,
            $"Approved — issued as {entity.Reference}.",
            projectId: entity.ProjectId,
            recordType: RecordType.WorkOrder,
            recordId: entity.WorkOrderId,
            recordReference: entity.Reference,
            actorEmail: command.ApprovedByEmail,
            cancellationToken: cancellationToken);

        return entity.ToModel();
    }
}
