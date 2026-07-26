using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

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

    private readonly BlobContainerClient container;
    private readonly SemaphoreSlim ensureContainerGate = new(1, 1);
    private bool containerEnsured;

    public AzureBlobRequestAttachmentStore(string connectionString)
    {
        // Bounded retry so a misconfigured storage account fails fast rather than appearing to
        // hang — the same reasoning as AzureBlobDrawingStore. This one matters more: the person
        // waiting is standing on site on a phone.
        var options = new BlobClientOptions
        {
            Retry =
            {
                Mode = Azure.Core.RetryMode.Fixed,
                MaxRetries = 2,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(3),
                NetworkTimeout = TimeSpan.FromSeconds(30),
            }
        };
        container = new BlobContainerClient(connectionString, ContainerName, options);
    }

    public async Task<string> UploadAsync(
        string projectId, string requestId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "attachment";
        var blobRef = $"{projectId}/{requestId}/{attachmentId}/{safeName}";

        var blob = container.GetBlobClient(blobRef);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);
        return blobRef;
    }

    public async Task<RequestAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobRef);
        if (!await blob.ExistsAsync(cancellationToken)) return null;

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var contentType = download.Value.Details.ContentType;
        return new RequestAttachmentBlob(
            download.Value.Content,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            download.Value.Details.ContentLength);
    }

    public async Task DeleteAsync(string blobRef, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobRef);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (containerEnsured) return;
        await ensureContainerGate.WaitAsync(cancellationToken);
        try
        {
            if (!containerEnsured)
            {
                await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
                containerEnsured = true;
            }
        }
        finally
        {
            ensureContainerGate.Release();
        }
    }
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
