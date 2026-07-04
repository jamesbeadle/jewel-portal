namespace Jewel.JPMS.Api.Features.Drawings.Storage;

/// <summary>
/// Fallback used when no storage connection string is configured. Any attempt to store or read a
/// file fails loudly so a misconfigured environment is obvious rather than silently losing files.
/// </summary>
public sealed class NullDrawingBlobStore : IDrawingBlobStore
{
    private const string Message =
        "Drawing file storage is not configured. Set 'DrawingsStorage:ConnectionString' (or 'AzureWebJobsStorage').";

    public Task<string> UploadAsync(
        string projectId, string drawingId, string revisionId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(Message);

    public Task<DrawingBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(Message);

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(Message);
}
