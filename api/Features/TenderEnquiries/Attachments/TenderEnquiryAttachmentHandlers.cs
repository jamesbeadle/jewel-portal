using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.TenderEnquiries;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Attachments;

internal static class TenderEnquiryAttachmentReader
{
    /// <summary>The enquiry's attachments in the order they were added.</summary>
    public static async Task<IReadOnlyList<TenderEnquiryAttachment>> ListAsync(
        JpmsContext context, string tenderEnquiryId, CancellationToken cancellationToken)
    {
        var rows = await context.TenderEnquiryAttachments.AsNoTracking()
            .Where(row => row.TenderEnquiryId == tenderEnquiryId)
            .OrderBy(row => row.AddedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.ToModel()).ToList();
    }
}

public sealed class ListTenderEnquiryAttachmentsHandler
    : IQueryHandler<ListTenderEnquiryAttachments, IReadOnlyList<TenderEnquiryAttachment>>
{
    private readonly JpmsContext context;
    public ListTenderEnquiryAttachmentsHandler(JpmsContext context) { this.context = context; }

    public Task<IReadOnlyList<TenderEnquiryAttachment>> HandleAsync(
        ListTenderEnquiryAttachments query, CancellationToken cancellationToken) =>
        TenderEnquiryAttachmentReader.ListAsync(context, query.TenderEnquiryId, cancellationToken);
}

public sealed class RemoveTenderEnquiryAttachmentHandler
    : ICommandHandler<RemoveTenderEnquiryAttachment, IReadOnlyList<TenderEnquiryAttachment>>
{
    private readonly JpmsContext context;
    private readonly ITenderEnquiryAttachmentStore blobStore;

    public RemoveTenderEnquiryAttachmentHandler(JpmsContext context, ITenderEnquiryAttachmentStore blobStore)
    {
        this.context = context;
        this.blobStore = blobStore;
    }

    public async Task<IReadOnlyList<TenderEnquiryAttachment>> HandleAsync(
        RemoveTenderEnquiryAttachment command, CancellationToken cancellationToken)
    {
        var entity = await context.TenderEnquiryAttachments.FirstOrDefaultAsync(
            row => row.TenderEnquiryAttachmentId == command.TenderEnquiryAttachmentId
                && row.TenderEnquiryId == command.TenderEnquiryId,
            cancellationToken);
        if (entity is null)
            return await TenderEnquiryAttachmentReader.ListAsync(context, command.TenderEnquiryId, cancellationToken);

        var blobRef = entity.BlobRef;
        context.TenderEnquiryAttachments.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        // The row is the record; the bytes go best-effort. An orphaned blob is harmless (private
        // container, never listed), whereas failing the remove over storage noise would leave a
        // row the user has already decided to tidy away.
        if (!string.IsNullOrWhiteSpace(blobRef))
        {
            try { await blobStore.DeleteAsync(blobRef, cancellationToken); }
            catch (Exception ex) when (ex is not OperationCanceledException) { }
        }
        return await TenderEnquiryAttachmentReader.ListAsync(context, command.TenderEnquiryId, cancellationToken);
    }
}
