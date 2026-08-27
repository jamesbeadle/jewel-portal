using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai.Storage;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Attachments;

/// <summary>
/// Copies files off an assistant conversation onto a work order's attachment register — the quote
/// the order was drafted from, kept for reference exactly like a file picked from disk. The bytes
/// move server-side, ai-attachments store → work-order store (a COPY: the chat keeps its own,
/// because the assistant may still be asked to read it, and the chat store's retention rule must
/// never quietly empty an order's records). Rows land with
/// <see cref="WorkOrderAttachmentSource.Chat"/> so the register says where each file came from.
///
/// <para>Guards, in order: the order must exist; the conversation must belong to the caller (an
/// id is not a capability — the same rule as every conversation read); and a file already copied
/// onto this order (same name and size, chat-sourced) is skipped rather than doubled, so saving
/// an edit twice cannot litter the register. A file whose blob the retention rule has already
/// reached refuses with its name — silently keeping a register row with no bytes behind it would
/// be a record that lies.</para>
/// </summary>
public sealed class AttachChatFilesToWorkOrderHandler
    : ICommandHandler<AttachChatFilesToWorkOrder, IReadOnlyList<WorkOrderAttachment>>
{
    private readonly JpmsContext context;
    private readonly IAiAttachmentStore chatStore;
    private readonly IWorkOrderAttachmentStore orderStore;

    public AttachChatFilesToWorkOrderHandler(
        JpmsContext context, IAiAttachmentStore chatStore, IWorkOrderAttachmentStore orderStore)
    {
        this.context = context;
        this.chatStore = chatStore;
        this.orderStore = orderStore;
    }

    public async Task<IReadOnlyList<WorkOrderAttachment>> HandleAsync(
        AttachChatFilesToWorkOrder command, CancellationToken cancellationToken)
    {
        var order = await context.WorkOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.WorkOrderId == command.WorkOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Work order {command.WorkOrderId} not found.");

        var conversationOwned = await context.AiConversations
            .AsNoTracking()
            .AnyAsync(row => row.ConversationId == command.ConversationId
                             && row.StartedByEmail == command.RequestedByEmail, cancellationToken);
        if (!conversationOwned)
            throw new InvalidOperationException("That chat could not be found — its files can't be copied.");

        var wanted = command.AttachmentIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (wanted.Count == 0)
            return await WorkOrderAttachmentMapping.ListAsync(context, command.WorkOrderId, cancellationToken);

        var sources = await context.AiAttachments
            .AsNoTracking()
            .Where(row => row.ConversationId == command.ConversationId && wanted.Contains(row.AttachmentId))
            .OrderBy(row => row.UploadedAt)
            .ToListAsync(cancellationToken);
        if (sources.Count < wanted.Count)
            throw new InvalidOperationException(
                "One of those chat files is no longer on the conversation — refresh and try again.");

        // What the order already holds from a chat, so a second save of the same dialog is a
        // no-op for the files that landed the first time.
        var alreadyCopied = await context.WorkOrderAttachments
            .AsNoTracking()
            .Where(row => row.WorkOrderId == command.WorkOrderId
                          && row.Source == (int)WorkOrderAttachmentSource.Chat)
            .Select(row => new { row.FileName, row.FileSizeBytes })
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var source in sources)
        {
            if (alreadyCopied.Any(existing =>
                    string.Equals(existing.FileName, source.FileName, StringComparison.OrdinalIgnoreCase)
                    && existing.FileSizeBytes == source.SizeBytes))
            {
                continue;
            }

            byte[]? bytes;
            try
            {
                bytes = await chatStore.OpenAsync(source.BlobRef, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await context.SaveChangesAsync(cancellationToken); // keep the files that DID copy
                throw new InvalidOperationException(
                    $"\"{source.FileName}\" could not be fetched from the chat's storage ({ex.Message}).");
            }
            if (bytes is null)
            {
                await context.SaveChangesAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"\"{source.FileName}\" is no longer held on the chat — attachments are kept for a "
                    + "limited time. Add it to the order with \"Add files\" instead.");
            }

            var attachmentId = Guid.NewGuid().ToString("N");
            var contentType = string.IsNullOrWhiteSpace(source.ContentType)
                ? "application/octet-stream"
                : source.ContentType;

            string blobRef;
            try
            {
                using var content = new MemoryStream(bytes, writable: false);
                blobRef = await orderStore.UploadAsync(
                    order.ProjectId, command.WorkOrderId, attachmentId,
                    source.FileName, contentType, content, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await context.SaveChangesAsync(cancellationToken); // keep the files that DID copy
                throw new InvalidOperationException($"Could not store \"{source.FileName}\". ({ex.Message})");
            }

            context.WorkOrderAttachments.Add(new WorkOrderAttachmentEntity
            {
                WorkOrderAttachmentId = attachmentId,
                WorkOrderId = command.WorkOrderId,
                ProjectId = order.ProjectId,
                FileName = source.FileName,
                ContentType = contentType,
                FileSizeBytes = source.SizeBytes,
                BlobRef = blobRef,
                Source = (int)WorkOrderAttachmentSource.Chat,
                AddedAt = now,
                AddedByEmail = command.RequestedByEmail
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return await WorkOrderAttachmentMapping.ListAsync(context, command.WorkOrderId, cancellationToken);
    }
}
