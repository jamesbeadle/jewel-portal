namespace Jewel.JPMS.Api.Features.DocumentControl.Storage;

/// <summary>No-op store used when blob storage isn't configured: uploads fail loudly (a Document
/// Control item without its file would be a lie) and opens return null.</summary>
public sealed class NullDocumentControlBlobStore : IDocumentControlBlobStore
{
    public Task<string> UploadItemAsync(
        string documentControlItemId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Document storage is not configured for this API.");

    public Task<string> UploadPaymentCertificateAsync(
        string projectId, string paymentCertificateId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Document storage is not configured for this API.");

    public Task<DocumentControlBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
        Task.FromResult<DocumentControlBlob?>(null);

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) => Task.CompletedTask;
}
