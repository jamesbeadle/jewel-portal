using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// Deletes a draft work order — undecided or rejected — with everything that only exists
/// under it: priced lines and attachments (rows and blobs). Never a live order: approval
/// minted a number the supplier has seen, so a live order is cancelled and its row kept.
/// The rows are gone afterwards, so the audit event is the surviving record (mirroring
/// ProjectDeleted). Idempotent on a missing id, like DeleteBidPackage.
/// </summary>
public sealed class DeleteDraftWorkOrderHandler : ICommandHandler<DeleteDraftWorkOrder, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly Attachments.IWorkOrderAttachmentStore attachmentStore;
    private readonly AuditTrail audit;
    private readonly ILogger<DeleteDraftWorkOrderHandler> logger;

    public DeleteDraftWorkOrderHandler(JpmsContext context,
        Attachments.IWorkOrderAttachmentStore attachmentStore, AuditTrail audit,
        ILogger<DeleteDraftWorkOrderHandler> logger)
    {
        this.context = context; this.attachmentStore = attachmentStore;
        this.audit = audit; this.logger = logger;
    }

    public async Task<Acknowledgement> HandleAsync(DeleteDraftWorkOrder command, CancellationToken cancellationToken)
    {
        var order = await context.WorkOrders.FindAsync(new object[] { command.WorkOrderId }, cancellationToken);
        if (order is null) return new Acknowledgement(command.WorkOrderId); // already gone — nothing to do
        if (!string.Equals(order.ProjectId, command.ProjectId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This work order does not belong to this project.");
        if (order.Status != (int)WorkOrderStatus.Draft && order.Status != (int)WorkOrderStatus.Rejected)
            throw new InvalidOperationException(
                "Only draft or rejected work orders can be deleted — this one was issued. Cancel a live order instead; its number is on record with the supplier.");

        var lines = await context.WorkOrderLines
            .Where(line => line.WorkOrderId == order.WorkOrderId)
            .ToListAsync(cancellationToken);
        context.WorkOrderLines.RemoveRange(lines);

        var attachments = await context.WorkOrderAttachments
            .Where(attachment => attachment.WorkOrderId == order.WorkOrderId)
            .ToListAsync(cancellationToken);
        context.WorkOrderAttachments.RemoveRange(attachments);

        var deletedTitle = order.Title;
        var wasRejected = order.Status == (int)WorkOrderStatus.Rejected;
        context.WorkOrders.Remove(order);
        await context.SaveChangesAsync(cancellationToken);

        // Blob clean-up is best-effort AFTER the rows are gone: an orphaned blob is a pennies
        // problem, a deleted blob whose row survived a failed save would be a broken download.
        foreach (var attachment in attachments)
        {
            try { await attachmentStore.DeleteAsync(attachment.BlobRef, cancellationToken); }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Couldn't delete work order attachment blob {BlobRef}.", attachment.BlobRef);
            }
        }

        // Best-effort after the save, per the AuditTrail convention — the row itself is the
        // surviving record of the draft, so the detail carries what the register no longer can.
        await audit.WriteAsync(
            AuditEventType.DraftWorkOrderDeleted,
            wasRejected
                ? $"Rejected work order \"{deletedTitle}\" was permanently deleted — no order number was ever minted."
                : $"Draft work order \"{deletedTitle}\" was permanently deleted before any decision — no order number was ever minted.",
            projectId: command.ProjectId,
            recordType: RecordType.WorkOrder,
            recordId: command.WorkOrderId,
            cancellationToken: cancellationToken);

        return new Acknowledgement(command.WorkOrderId);
    }
}
