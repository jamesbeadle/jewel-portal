using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Attachments;

/// <summary>One file about to be stored on an enquiry, wherever it came from.</summary>
public sealed record TenderEnquiryIncomingFile(string Name, string ContentType, byte[] Content);

/// <summary>
/// The one way a file lands on an enquiry: bytes into the private container, a register row in
/// the same context. Shared by the multipart upload endpoint and the copy-off-the-email path so
/// the two can never record a file differently. Callers save the context.
/// </summary>
public sealed class TenderEnquiryAttachmentWriter
{
    private const string FallbackFileName = "attachment";
    private const string FallbackContentType = "application/octet-stream";

    private readonly JpmsContext context;
    private readonly ITenderEnquiryAttachmentStore blobStore;

    public TenderEnquiryAttachmentWriter(JpmsContext context, ITenderEnquiryAttachmentStore blobStore)
    {
        this.context = context;
        this.blobStore = blobStore;
    }

    public async Task StoreAsync(
        TenderEnquiryEntity enquiry, TenderEnquiryIncomingFile file, TenderEnquiryAttachmentSource source,
        string addedByEmail, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(file.Content, writable: false);
        await StoreAsync(enquiry, file.Name, file.ContentType, file.Content.LongLength, stream, source, addedByEmail, cancellationToken);
    }

    public async Task StoreAsync(
        TenderEnquiryEntity enquiry, string fileName, string contentType, long length, Stream content,
        TenderEnquiryAttachmentSource source, string addedByEmail, CancellationToken cancellationToken)
    {
        var attachmentId = Guid.NewGuid().ToString("N");
        var safeFileName = string.IsNullOrWhiteSpace(fileName) ? FallbackFileName : fileName;
        var safeContentType = string.IsNullOrWhiteSpace(contentType) ? FallbackContentType : contentType;

        var blobRef = await blobStore.UploadAsync(
            enquiry.ProjectId, enquiry.TenderEnquiryId, attachmentId, safeFileName, safeContentType, content, cancellationToken);

        context.TenderEnquiryAttachments.Add(new TenderEnquiryAttachmentEntity
        {
            TenderEnquiryAttachmentId = attachmentId,
            TenderEnquiryId = enquiry.TenderEnquiryId,
            ProjectId = enquiry.ProjectId,
            FileName = safeFileName,
            ContentType = safeContentType,
            FileSizeBytes = length,
            BlobRef = blobRef,
            Source = (int)source,
            AddedAt = DateTimeOffset.UtcNow,
            AddedByEmail = addedByEmail
        });
    }
}
