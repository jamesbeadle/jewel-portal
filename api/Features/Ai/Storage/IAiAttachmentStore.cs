namespace Jewel.JPMS.Api.Features.Ai.Storage;

/// <summary>
/// Where a chat attachment's bytes live between the upload and every later read. One private
/// container, one blob per attachment, keyed by conversation and attachment id. Kept as narrow as
/// the other five stores: upload, open, delete.
/// </summary>
public interface IAiAttachmentStore
{
    /// <summary>True when a real store is configured. The upload handler refuses attachments
    /// otherwise — a file the assistant can never read back is worse than an honest "not
    /// configured".</summary>
    bool IsConfigured { get; }

    /// <summary>Stores the bytes and returns the blob reference to persist on the row.</summary>
    Task<string> UploadAsync(
        string conversationId, string attachmentId, string fileName, string contentType,
        byte[] content, CancellationToken cancellationToken);

    /// <summary>The bytes, or null when the blob is gone (the retention rule reached it).</summary>
    Task<byte[]?> OpenAsync(string blobRef, CancellationToken cancellationToken);

    Task DeleteAsync(string blobRef, CancellationToken cancellationToken);
}
