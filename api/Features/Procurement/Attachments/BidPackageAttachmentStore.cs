using Jewel.JPMS.Api.Storage;

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

    private readonly AzureBlobFileStore files;

    public AzureBlobBidPackageAttachmentStore(string connectionString) =>
        files = new AzureBlobFileStore(connectionString, ContainerName);

    public Task<string> UploadAsync(
        string projectId, string bidPackageId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        files.UploadAsync(
            $"{projectId}/{bidPackageId}/{attachmentId}/{AzureBlobFileStore.SafeFileName(fileName)}",
            contentType, content, cancellationToken);

    public async Task<BidPackageAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
        await files.OpenAsync(blobRef, cancellationToken) is { } blob
            ? new BidPackageAttachmentBlob(blob.Content, blob.ContentType, blob.Length)
            : null;

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) =>
        files.DeleteAsync(blobRef, cancellationToken);
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
