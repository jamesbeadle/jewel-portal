namespace Jewel.JPMS.Api.Features.Subcontractors.Storage;

/// <summary>
/// Stores and retrieves the binary file behind a compliance document. The container is private;
/// downloads are proxied through the API rather than handed out as public URLs (mirrors
/// IDrawingBlobStore).
/// </summary>
public interface IComplianceBlobStore
{
    /// <summary>Uploads a document's file and returns the blob path to persist on the record.</summary>
    Task<string> UploadAsync(
        string subcontractorId, string complianceDocumentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    /// <summary>Opens a stored file by its blob path, or null if it no longer exists.</summary>
    Task<ComplianceBlob?> OpenAsync(string blobPath, CancellationToken cancellationToken);

    /// <summary>Deletes a stored file by its blob path. Deleting a missing blob is a no-op.</summary>
    Task DeleteAsync(string blobPath, CancellationToken cancellationToken);
}

public sealed record ComplianceBlob(Stream Content, string ContentType, long Length);
