using Jewel.JPMS.Api.Storage;

namespace Jewel.JPMS.Api.Features.Requests.Attachments;

/// <summary>
/// Stores the files uploaded onto a request — in practice site photographs. Private container,
/// downloads proxied through the API, same contract as the drawings and progress-photo stores.
/// </summary>
public interface IRequestAttachmentStore
{
    Task<string> UploadAsync(
        string projectId, string requestId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    Task<RequestAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken);

    Task DeleteAsync(string blobRef, CancellationToken cancellationToken);
}

public sealed record RequestAttachmentBlob(Stream Content, string ContentType, long Length);

/// <summary>
/// Azure Blob Storage implementation. Keyed <c>{projectId}/{requestId}/{attachmentId}/{fileName}</c>
/// in a private container created on first use.
/// </summary>
public sealed class AzureBlobRequestAttachmentStore : IRequestAttachmentStore
{
    public const string ContainerName = "request-attachments";

    private readonly AzureBlobFileStore files;

    public AzureBlobRequestAttachmentStore(string connectionString) =>
        files = new AzureBlobFileStore(connectionString, ContainerName);

    public Task<string> UploadAsync(
        string projectId, string requestId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        files.UploadAsync(
            $"{projectId}/{requestId}/{attachmentId}/{AzureBlobFileStore.SafeFileName(fileName)}",
            contentType, content, cancellationToken);

    public async Task<RequestAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
        await files.OpenAsync(blobRef, cancellationToken) is { } blob
            ? new RequestAttachmentBlob(blob.Content, blob.ContentType, blob.Length)
            : null;

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) =>
        files.DeleteAsync(blobRef, cancellationToken);
}

/// <summary>
/// No-storage fallback. Linking drawings still works (nothing is stored for a link); uploading a
/// photo fails loudly rather than silently recording a row with no file behind it.
/// </summary>
public sealed class NullRequestAttachmentStore : IRequestAttachmentStore
{
    public Task<string> UploadAsync(
        string projectId, string requestId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(
            "No file storage is configured, so the photo can't be saved. " +
            "Set RequestAttachmentsStorage:ConnectionString (or AzureWebJobsStorage) and try again.");

    public Task<RequestAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
        Task.FromResult<RequestAttachmentBlob?>(null);

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) => Task.CompletedTask;
}
