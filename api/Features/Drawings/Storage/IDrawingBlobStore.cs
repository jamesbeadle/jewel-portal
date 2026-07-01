namespace Jewel.JPMS.Api.Features.Drawings.Storage;

/// <summary>
/// Stores and retrieves the binary file behind a drawing revision. The container is private;
/// downloads are proxied through the API rather than handed out as public URLs.
/// </summary>
public interface IDrawingBlobStore
{
    /// <summary>Uploads a revision's file and returns the blob reference to persist on the revision.</summary>
    Task<string> UploadAsync(
        string projectId, string drawingId, string revisionId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    /// <summary>Opens a stored file by its blob reference, or null if it no longer exists.</summary>
    Task<DrawingBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken);
}

public sealed record DrawingBlob(Stream Content, string ContentType, long Length);
