namespace Jewel.JPMS.Api.Features.BuildingControl.Attachments;

/// <summary>
/// Stores the files kept on a building control case or inspection — stage photos, the
/// inspector's site reports, the notices and the completion certificate. Private container,
/// downloads proxied through the API — the tender-enquiry attachment contract.
/// </summary>
public interface IBuildingControlAttachmentStore
{
    Task<string> UploadAsync(
        string projectId, string parentId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken);

    Task<BuildingControlAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken);

    Task DeleteAsync(string blobRef, CancellationToken cancellationToken);
}

public sealed record BuildingControlAttachmentBlob(Stream Content, string ContentType, long Length);

/// <summary>
/// No-storage fallback. Listing still works (a stage with no photos is a real answer); storing a
/// file fails loudly rather than silently recording a row with no file behind it.
/// </summary>
public sealed class NullBuildingControlAttachmentStore : IBuildingControlAttachmentStore
{
    public Task<string> UploadAsync(
        string projectId, string parentId, string attachmentId,
        string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(
            "No file storage is configured, so the attachment can't be saved. " +
            "Set BuildingControlAttachmentsStorage:ConnectionString (or AzureWebJobsStorage) and try again.");

    public Task<BuildingControlAttachmentBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
        Task.FromResult<BuildingControlAttachmentBlob?>(null);

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) => Task.CompletedTask;
}
