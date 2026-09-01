using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Attachments;

internal static class BidPackageAttachmentMapping
{
    public static BidPackageAttachment ToModel(this BidPackageAttachmentEntity entity) =>
        new(
            entity.BidPackageAttachmentId,
            entity.BidPackageId,
            entity.ProjectId,
            entity.FileName,
            entity.ContentType,
            entity.FileSizeBytes,
            (BidPackageAttachmentSource)entity.Source,
            entity.AddedAt,
            entity.AddedByEmail);

    /// <summary>The package's attachments in the order they were added.</summary>
    public static async Task<IReadOnlyList<BidPackageAttachment>> ListAsync(
        JpmsContext context, string bidPackageId, CancellationToken cancellationToken)
    {
        var rows = await context.BidPackageAttachments
            .AsNoTracking()
            .Where(row => row.BidPackageId == bidPackageId)
            .OrderBy(row => row.AddedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.ToModel()).ToList();
    }
}

public sealed class ListBidPackageAttachmentsHandler
    : IQueryHandler<ListBidPackageAttachments, IReadOnlyList<BidPackageAttachment>>
{
    private readonly JpmsContext context;
    public ListBidPackageAttachmentsHandler(JpmsContext context) { this.context = context; }

    public Task<IReadOnlyList<BidPackageAttachment>> HandleAsync(
        ListBidPackageAttachments query, CancellationToken cancellationToken) =>
        BidPackageAttachmentMapping.ListAsync(context, query.BidPackageId, cancellationToken);
}

public sealed class RemoveBidPackageAttachmentHandler
    : ICommandHandler<RemoveBidPackageAttachment, IReadOnlyList<BidPackageAttachment>>
{
    private readonly JpmsContext context;
    private readonly IBidPackageAttachmentStore blobStore;

    public RemoveBidPackageAttachmentHandler(JpmsContext context, IBidPackageAttachmentStore blobStore)
    {
        this.context = context;
        this.blobStore = blobStore;
    }

    public async Task<IReadOnlyList<BidPackageAttachment>> HandleAsync(
        RemoveBidPackageAttachment command, CancellationToken cancellationToken)
    {
        var entity = await context.BidPackageAttachments
            .FirstOrDefaultAsync(
                row => row.BidPackageAttachmentId == command.BidPackageAttachmentId
                    && row.BidPackageId == command.BidPackageId,
                cancellationToken);
        if (entity is null)
            return await BidPackageAttachmentMapping.ListAsync(context, command.BidPackageId, cancellationToken);

        var blobRef = entity.BlobRef;
        context.BidPackageAttachments.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        // The row is the record; the bytes go best-effort. An orphaned blob is harmless (private
        // container, never listed), whereas failing the remove over storage noise would leave a
        // row the user has already decided to tidy away.
        if (!string.IsNullOrWhiteSpace(blobRef))
        {
            try { await blobStore.DeleteAsync(blobRef, cancellationToken); }
            catch (Exception ex) when (ex is not OperationCanceledException) { }
        }

        return await BidPackageAttachmentMapping.ListAsync(context, command.BidPackageId, cancellationToken);
    }
}
