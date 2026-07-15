namespace Jewel.JPMS.Api.Features.Progress.Storage;

/// <summary>
/// Stores and retrieves the image files behind progress updates. The container is private;
/// downloads are proxied through the API rather than handed out as public URLs.
/// </summary>
public interface IProgressPhotoStore
{
    /// <summary>Uploads a photo and returns the blob reference to persist on the photo row.</summary>
    Task<string> UploadAsync(
        string projectId, string progressUpdateId, string photoId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    /// <summary>Opens a stored photo by its blob reference, or null if it no longer exists.</summary>
    Task<ProgressPhotoBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken);

    /// <summary>Deletes a stored photo by its blob reference. Deleting a missing blob is a no-op.</summary>
    Task DeleteAsync(string blobRef, CancellationToken cancellationToken);
}

public sealed record ProgressPhotoBlob(Stream Content, string ContentType, long Length);
