using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Attachments;

/// <summary>
/// Stores the files kept on a work order for record keeping — quotes, signed copies, photos of
/// scope. Private container, downloads proxied through the API, same contract as the request
/// attachment, drawings and progress-photo stores. These files never reach the supplier: the
/// purchase-order email and printed PO ignore them entirely.
/// </summary>
public interface IWorkOrderAttachmentStore
{
    Task<string> UploadAsync(
        string projectId, string workOrderId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    Task<WorkOrderAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken);

    Task DeleteAsync(string blobRef, CancellationToken cancellationToken);
}

public sealed record WorkOrderAttachmentBlob(Stream Content, string ContentType, long Length);

/// <summary>
/// Azure Blob Storage implementation. Keyed <c>{projectId}/{workOrderId}/{attachmentId}/{fileName}</c>
/// in a private container created on first use.
/// </summary>
public sealed class AzureBlobWorkOrderAttachmentStore : IWorkOrderAttachmentStore
{
    public const string ContainerName = "work-order-attachments";

    private readonly BlobContainerClient container;
    private readonly SemaphoreSlim ensureContainerGate = new(1, 1);
    private bool containerEnsured;

    public AzureBlobWorkOrderAttachmentStore(string connectionString)
    {
        // Bounded retry so a misconfigured storage account fails fast rather than appearing to
        // hang — the same reasoning as AzureBlobRequestAttachmentStore.
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
        string projectId, string workOrderId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "attachment";
        var blobRef = $"{projectId}/{workOrderId}/{attachmentId}/{safeName}";

        var blob = container.GetBlobClient(blobRef);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);
        return blobRef;
    }

    public async Task<WorkOrderAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobRef);
        if (!await blob.ExistsAsync(cancellationToken)) return null;

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var contentType = download.Value.Details.ContentType;
        return new WorkOrderAttachmentBlob(
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
/// No-storage fallback. Listing still works (an order with no attachments is a real answer);
/// storing a file fails loudly rather than silently recording a row with no file behind it.
/// </summary>
public sealed class NullWorkOrderAttachmentStore : IWorkOrderAttachmentStore
{
    public Task<string> UploadAsync(
        string projectId, string workOrderId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(
            "No file storage is configured, so the attachment can't be saved. " +
            "Set WorkOrderAttachmentsStorage:ConnectionString (or AzureWebJobsStorage) and try again.");

    public Task<WorkOrderAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
        Task.FromResult<WorkOrderAttachmentBlob?>(null);

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) => Task.CompletedTask;
}
