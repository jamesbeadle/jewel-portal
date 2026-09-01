using Jewel.JPMS.Api.Storage;

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

    private readonly AzureBlobFileStore files;

    public AzureBlobWorkOrderAttachmentStore(string connectionString) =>
        files = new AzureBlobFileStore(connectionString, ContainerName);

    public Task<string> UploadAsync(
        string projectId, string workOrderId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        files.UploadAsync(
            $"{projectId}/{workOrderId}/{attachmentId}/{AzureBlobFileStore.SafeFileName(fileName)}",
            contentType, content, cancellationToken);

    public async Task<WorkOrderAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
        await files.OpenAsync(blobRef, cancellationToken) is { } blob
            ? new WorkOrderAttachmentBlob(blob.Content, blob.ContentType, blob.Length)
            : null;

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) =>
        files.DeleteAsync(blobRef, cancellationToken);
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
