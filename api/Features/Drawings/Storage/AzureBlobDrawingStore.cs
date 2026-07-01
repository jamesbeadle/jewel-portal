using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Jewel.JPMS.Api.Features.Drawings.Storage;

/// <summary>
/// Azure Blob Storage implementation. Files live in a single private container, keyed
/// <c>{projectId}/{drawingId}/{revisionId}/{fileName}</c>. The container is created on first use.
/// No file-size cap is enforced here — Azure block-blob limits apply.
/// </summary>
public sealed class AzureBlobDrawingStore : IDrawingBlobStore
{
    public const string ContainerName = "drawings";

    private readonly BlobContainerClient container;
    private readonly SemaphoreSlim ensureContainerGate = new(1, 1);
    private bool containerEnsured;

    public AzureBlobDrawingStore(string connectionString)
    {
        container = new BlobContainerClient(connectionString, ContainerName);
    }

    public async Task<string> UploadAsync(
        string projectId, string drawingId, string revisionId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        var blobRef = BuildBlobRef(projectId, drawingId, revisionId, fileName);
        var blob = container.GetBlobClient(blobRef);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);
        return blobRef;
    }

    public async Task<DrawingBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobRef);
        if (!await blob.ExistsAsync(cancellationToken)) return null;

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var contentType = download.Value.Details.ContentType;
        var length = download.Value.Details.ContentLength;
        return new DrawingBlob(
            download.Value.Content,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            length);
    }

    private static string BuildBlobRef(string projectId, string drawingId, string revisionId, string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "file";
        return $"{projectId}/{drawingId}/{revisionId}/{safeName}";
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
