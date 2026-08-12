using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Jewel.JPMS.Api.Features.DocumentControl.Storage;

/// <summary>
/// Azure Blob Storage implementation. Files live in a single private container: queue items keyed
/// <c>items/{itemId}/{fileName}</c>, payment certificate copies keyed
/// <c>payment-certificates/{projectId}/{certificateId}/{fileName}</c>. The container is created on
/// first use. Mirrors AzureBlobDrawingStore, bounded retries included.
/// </summary>
public sealed class AzureBlobDocumentControlStore : IDocumentControlBlobStore
{
    public const string ContainerName = "document-control";

    private readonly BlobContainerClient container;
    private readonly SemaphoreSlim ensureContainerGate = new(1, 1);
    private bool containerEnsured;

    public AzureBlobDocumentControlStore(string connectionString)
    {
        // Bound the retry/backoff so an unreachable or misconfigured storage account surfaces as a
        // quick error instead of the SDK's default long retry chain (mirrors AzureBlobDrawingStore).
        var options = new BlobClientOptions
        {
            Retry =
            {
                Mode = Azure.Core.RetryMode.Fixed,
                MaxRetries = 2,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(3),
                NetworkTimeout = TimeSpan.FromSeconds(15),
            }
        };
        container = new BlobContainerClient(connectionString, ContainerName, options);
    }

    public Task<string> UploadItemAsync(
        string documentControlItemId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        UploadAsync($"items/{documentControlItemId}/{SafeName(fileName)}", contentType, content, cancellationToken);

    public Task<string> UploadPaymentCertificateAsync(
        string projectId, string paymentCertificateId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        UploadAsync(
            $"payment-certificates/{projectId}/{paymentCertificateId}/{SafeName(fileName)}",
            contentType, content, cancellationToken);

    public async Task<DocumentControlBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobRef);
        if (!await blob.ExistsAsync(cancellationToken)) return null;

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var contentType = download.Value.Details.ContentType;
        var length = download.Value.Details.ContentLength;
        return new DocumentControlBlob(
            download.Value.Content,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            length);
    }

    public async Task DeleteAsync(string blobRef, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobRef);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private async Task<string> UploadAsync(
        string blobRef, string contentType, Stream content, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        var blob = container.GetBlobClient(blobRef);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);
        return blobRef;
    }

    private static string SafeName(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(safeName) ? "file" : safeName;
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
