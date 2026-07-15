namespace Jewel.JPMS.Api.Features.Progress.Storage;

/// <summary>
/// Fallback used when no storage connection string is configured. Any attempt to store or read a
/// file fails loudly so a misconfigured environment is obvious rather than silently losing files.
/// </summary>
public sealed class NullProgressPhotoStore : IProgressPhotoStore
{
    private const string Message =
        "Progress photo storage is not configured. Set 'ProgressPhotosStorage:ConnectionString' (or 'AzureWebJobsStorage').";

    public Task<string> UploadAsync(
        string projectId, string progressUpdateId, string photoId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(Message);

    public Task<ProgressPhotoBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(Message);

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(Message);
}
