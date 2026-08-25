namespace Jewel.JPMS.Api.Features.TenderEnquiries.Attachments;

/// <summary>
/// Stores the files kept on a tender enquiry — the questionnaire as received, the architect's
/// drawings, Jewel's supporting material. Private container, downloads proxied through the API,
/// the same contract as the bid-package attachment store.
/// </summary>
public interface ITenderEnquiryAttachmentStore
{
    Task<string> UploadAsync(
        string projectId, string tenderEnquiryId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    Task<TenderEnquiryAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken);

    Task DeleteAsync(string blobRef, CancellationToken cancellationToken);
}

public sealed record TenderEnquiryAttachmentBlob(Stream Content, string ContentType, long Length);

/// <summary>
/// No-storage fallback. Listing still works (an enquiry with no attachments is a real answer);
/// storing a file fails loudly rather than silently recording a row with no file behind it.
/// </summary>
public sealed class NullTenderEnquiryAttachmentStore : ITenderEnquiryAttachmentStore
{
    public Task<string> UploadAsync(
        string projectId, string tenderEnquiryId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(
            "No file storage is configured, so the attachment can't be saved. " +
            "Set TenderEnquiryAttachmentsStorage:ConnectionString (or AzureWebJobsStorage) and try again.");

    public Task<TenderEnquiryAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
        Task.FromResult<TenderEnquiryAttachmentBlob?>(null);

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) => Task.CompletedTask;
}
