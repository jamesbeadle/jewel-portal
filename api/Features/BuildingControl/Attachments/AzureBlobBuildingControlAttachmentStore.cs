using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Jewel.JPMS.Api.Features.BuildingControl.Attachments;

/// <summary>
/// Azure Blob Storage implementation. Keyed <c>{projectId}/{parentId}/{attachmentId}/{fileName}</c>
/// (parent = the case or the inspection) in a private container created on first use — the
/// tender-enquiry store's arrangement.
/// </summary>
public sealed class AzureBlobBuildingControlAttachmentStore : IBuildingControlAttachmentStore
{
    public const string ContainerName = "building-control-attachments";
    private const int MaxRetries = 2;

    private readonly BlobContainerClient container;
    private readonly SemaphoreSlim ensureContainerGate = new(1, 1);
    private bool isContainerEnsured;

    public AzureBlobBuildingControlAttachmentStore(string connectionString)
    {
        // Bounded retry so a misconfigured storage account fails fast rather than appearing to
        // hang — the same reasoning as the bid-package store.
        var options = new BlobClientOptions
        {
            Retry =
            {
                Mode = Azure.Core.RetryMode.Fixed,
                MaxRetries = MaxRetries,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(3),
                NetworkTimeout = TimeSpan.FromSeconds(30),
            }
        };
        container = new BlobContainerClient(connectionString, ContainerName, options);
    }

    public async Task<string> UploadAsync(
        string projectId, string parentId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "attachment";
        var blobRef = $"{projectId}/{parentId}/{attachmentId}/{safeName}";

        var blob = container.GetBlobClient(blobRef);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);
        return blobRef;
    }

    public async Task<BuildingControlAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobRef);
        if (!await blob.ExistsAsync(cancellationToken)) return null;

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var contentType = download.Value.Details.ContentType;
        return new BuildingControlAttachmentBlob(
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
        if (isContainerEnsured) return;
        await ensureContainerGate.WaitAsync(cancellationToken);
        try
        {
            if (isContainerEnsured) return;
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
            isContainerEnsured = true;
        }
        finally
        {
            ensureContainerGate.Release();
        }
    }
}
