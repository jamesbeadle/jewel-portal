using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Storage;

/// <summary>
/// Blob storage for executed contract documents. Private container, one blob per contract, keyed
/// {projectId}/{projectContractId}/{fileName}. Mirrors AzureBlobDrawingStore — including its bounded
/// retry policy, so a misconfigured storage account fails fast instead of hanging the request.
///
/// <para>No file-size cap is enforced here; Azure block-blob limits apply. The endpoint caps at
/// 100 MB, matching the compliance upload.</para>
/// </summary>
public sealed class AzureBlobProjectContractStore : IProjectContractBlobStore
{
    public const string ContainerName = "project-contracts";

    private readonly BlobContainerClient container;
    private readonly SemaphoreSlim ensureContainerGate = new(1, 1);
    private bool containerEnsured;

    public AzureBlobProjectContractStore(string connectionString)
    {
        var options = new BlobClientOptions
        {
            Retry =
            {
                Mode = Azure.Core.RetryMode.Fixed,
                MaxRetries = 2,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(3),
                // Per-attempt, so a large upload is unaffected by it.
                NetworkTimeout = TimeSpan.FromSeconds(15),
            }
        };
        container = new BlobContainerClient(connectionString, ContainerName, options);
    }

    public async Task<string> UploadAsync(
        string projectId, string projectContractId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        var blobRef = BuildBlobRef(projectId, projectContractId, fileName);
        var blob = container.GetBlobClient(blobRef);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);
        return blobRef;
    }

    public async Task<ProjectContractBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobRef);
        if (!await blob.ExistsAsync(cancellationToken)) return null;

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var contentType = download.Value.Details.ContentType;
        var length = download.Value.Details.ContentLength;
        return new ProjectContractBlob(
            download.Value.Content,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            length);
    }

    public async Task DeleteAsync(string blobRef, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobRef);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    // Path.GetFileName strips any directory the browser may have sent in the filename.
    private static string BuildBlobRef(string projectId, string projectContractId, string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "contract";
        return $"{projectId}/{projectContractId}/{safeName}";
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
