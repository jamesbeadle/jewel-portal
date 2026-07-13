using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Jewel.JPMS.Api.Features.Subcontractors.Storage;

/// <summary>
/// Azure Blob Storage implementation. Files live in a single private container, keyed
/// <c>{subcontractorId}/{complianceDocumentId}/{fileName}</c>. The container is created on first
/// use. Mirrors AzureBlobDrawingStore (bounded retries so a misconfigured account fails fast).
/// </summary>
public sealed class AzureBlobComplianceStore : IComplianceBlobStore
{
    public const string ContainerName = "compliance-documents";

    private readonly BlobContainerClient container;
    private readonly SemaphoreSlim ensureContainerGate = new(1, 1);
    private bool containerEnsured;

    public AzureBlobComplianceStore(string connectionString)
    {
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

    public async Task<string> UploadAsync(
        string subcontractorId, string complianceDocumentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        var blobPath = BuildBlobPath(subcontractorId, complianceDocumentId, fileName);
        var blob = container.GetBlobClient(blobPath);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);
        return blobPath;
    }

    public async Task<ComplianceBlob?> OpenAsync(string blobPath, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobPath);
        if (!await blob.ExistsAsync(cancellationToken)) return null;

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var contentType = download.Value.Details.ContentType;
        var length = download.Value.Details.ContentLength;
        return new ComplianceBlob(
            download.Value.Content,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            length);
    }

    public async Task DeleteAsync(string blobPath, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobPath);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private static string BuildBlobPath(string subcontractorId, string complianceDocumentId, string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "file";
        return $"{subcontractorId}/{complianceDocumentId}/{safeName}";
    }

    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (containerEnsured) return;
        await ensureContainerGate.WaitAsync(cancellationToken);
        try
        {
            if (containerEnsured) return;
            await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            containerEnsured = true;
        }
        finally
        {
            ensureContainerGate.Release();
        }
    }
}
