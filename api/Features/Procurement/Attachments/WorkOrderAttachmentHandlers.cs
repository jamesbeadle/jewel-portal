using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Attachments;

internal static class WorkOrderAttachmentMapping
{
    public static WorkOrderAttachment ToModel(this WorkOrderAttachmentEntity entity) =>
        new(
            entity.WorkOrderAttachmentId,
            entity.WorkOrderId,
            entity.ProjectId,
            entity.FileName,
            entity.ContentType,
            entity.FileSizeBytes,
            (WorkOrderAttachmentSource)entity.Source,
            entity.AddedAt,
            entity.AddedByEmail);

    /// <summary>The order's attachments in the order they were added.</summary>
    public static async Task<IReadOnlyList<WorkOrderAttachment>> ListAsync(
        JpmsContext context, string workOrderId, CancellationToken cancellationToken)
    {
        var rows = await context.WorkOrderAttachments
            .AsNoTracking()
            .Where(row => row.WorkOrderId == workOrderId)
            .OrderBy(row => row.AddedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.ToModel()).ToList();
    }
}

public sealed class ListWorkOrderAttachmentsHandler
    : IQueryHandler<ListWorkOrderAttachments, IReadOnlyList<WorkOrderAttachment>>
{
    private readonly JpmsContext context;
    public ListWorkOrderAttachmentsHandler(JpmsContext context) { this.context = context; }

    public Task<IReadOnlyList<WorkOrderAttachment>> HandleAsync(
        ListWorkOrderAttachments query, CancellationToken cancellationToken) =>
        WorkOrderAttachmentMapping.ListAsync(context, query.WorkOrderId, cancellationToken);
}

public sealed class RemoveWorkOrderAttachmentHandler
    : ICommandHandler<RemoveWorkOrderAttachment, IReadOnlyList<WorkOrderAttachment>>
{
    private readonly JpmsContext context;
    private readonly IWorkOrderAttachmentStore blobStore;

    public RemoveWorkOrderAttachmentHandler(JpmsContext context, IWorkOrderAttachmentStore blobStore)
    {
        this.context = context;
        this.blobStore = blobStore;
    }

    public async Task<IReadOnlyList<WorkOrderAttachment>> HandleAsync(
        RemoveWorkOrderAttachment command, CancellationToken cancellationToken)
    {
        var entity = await context.WorkOrderAttachments
            .FirstOrDefaultAsync(
                row => row.WorkOrderAttachmentId == command.WorkOrderAttachmentId
                    && row.WorkOrderId == command.WorkOrderId,
                cancellationToken);
        if (entity is null)
            return await WorkOrderAttachmentMapping.ListAsync(context, command.WorkOrderId, cancellationToken);

        var blobRef = entity.BlobRef;
        context.WorkOrderAttachments.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        // The row is the record; the bytes go best-effort. An orphaned blob is harmless (private
        // container, never listed), whereas failing the remove over storage noise would leave a
        // row the user has already decided to tidy away.
        if (!string.IsNullOrWhiteSpace(blobRef))
        {
            try { await blobStore.DeleteAsync(blobRef, cancellationToken); }
            catch (Exception ex) when (ex is not OperationCanceledException) { }
        }

        return await WorkOrderAttachmentMapping.ListAsync(context, command.WorkOrderId, cancellationToken);
    }
}
