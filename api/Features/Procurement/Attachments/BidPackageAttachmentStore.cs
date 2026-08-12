using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Attachments;

/// <summary>
/// Stores the files kept on a bid package as tender documents — specification extracts, schedules
/// of finishes, survey photos. Private container, downloads proxied through the API, same contract
/// as the work-order attachment store. Unlike work-order attachments these ARE supplier-facing:
/// PrepareBidPackageInviteDraft attaches them to the invite email alongside the linked drawings.
/// </summary>
public interface IBidPackageAttachmentStore
{
    Task<string> UploadAsync(
        string projectId, string bidPackageId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    Task<BidPackageAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken);

    Task DeleteAsync(string blobRef, CancellationToken cancellationToken);
}

public sealed record BidPackageAttachmentBlob(Stream Content, string ContentType, long Length);

/// <summary>
/// Azure Blob Storage implementation. Keyed <c>{projectId}/{bidPackageId}/{attachmentId}/{fileName}</c>
/// in a private container created on first use.
/// </summary>
public sealed class AzureBlobBidPackageAttachmentStore : IBidPackageAttachmentStore
{
    public const string ContainerName = "bid-package-attachments";

    private readonly BlobContainerClient container;
    private readonly SemaphoreSlim ensureContainerGate = new(1, 1);
    private bool containerEnsured;

    public AzureBlobBidPackageAttachmentStore(string connectionString)
    {
        // Bounded retry so a misconfigured storage account fails fast rather than appearing to
        // hang — the same reasoning as AzureBlobWorkOrderAttachmentStore.
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
        string projectId, string bidPackageId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);

        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "attachment";
        var blobRef = $"{projectId}/{bidPackageId}/{attachmentId}/{safeName}";

        var blob = container.GetBlobClient(blobRef);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);
        return blobRef;
    }

    public async Task<BidPackageAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken)
    {
        var blob = container.GetBlobClient(blobRef);
        if (!await blob.ExistsAsync(cancellationToken)) return null;

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var contentType = download.Value.Details.ContentType;
        return new BidPackageAttachmentBlob(
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
/// No-storage fallback. Listing still works (a package with no attachments is a real answer);
/// storing a file fails loudly rather than silently recording a row with no file behind it.
/// </summary>
public sealed class NullBidPackageAttachmentStore : IBidPackageAttachmentStore
{
    public Task<string> UploadAsync(
        string projectId, string bidPackageId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(
            "No file storage is configured, so the attachment can't be saved. " +
            "Set BidPackageAttachmentsStorage:ConnectionString (or AzureWebJobsStorage) and try again.");

    public Task<BidPackageAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
        Task.FromResult<BidPackageAttachmentBlob?>(null);

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) => Task.CompletedTask;
}
