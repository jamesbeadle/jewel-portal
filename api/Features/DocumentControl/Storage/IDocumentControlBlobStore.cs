namespace Jewel.JPMS.Api.Features.DocumentControl.Storage;

/// <summary>
/// Stores and retrieves the binary file behind a Document Control item — and the payment
/// certificate register's own copies. The container is private; downloads are proxied through the
/// API rather than handed out as public URLs (mirrors IDrawingBlobStore).
/// </summary>
public interface IDocumentControlBlobStore
{
    /// <summary>Uploads a queue item's file and returns the blob ref to persist on the row.</summary>
    Task<string> UploadItemAsync(
        string documentControlItemId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    /// <summary>Uploads a payment certificate's own copy and returns the blob ref to persist —
    /// separate from the queue item's blob, so queue housekeeping can never orphan the register.</summary>
    Task<string> UploadPaymentCertificateAsync(
        string projectId, string paymentCertificateId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    /// <summary>Opens a stored file by its blob ref, or null if it no longer exists.</summary>
    Task<DocumentControlBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken);

    /// <summary>Deletes a stored file by its blob ref. Deleting a missing blob is a no-op.</summary>
    Task DeleteAsync(string blobRef, CancellationToken cancellationToken);
}

public sealed record DocumentControlBlob(Stream Content, string ContentType, long Length);
