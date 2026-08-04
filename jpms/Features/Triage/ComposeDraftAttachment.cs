using Jewel.JPMS.Contracts.MailboxCompose;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Features.Triage;

/// <summary>
/// One attachment the composer is holding, before send. Key doubles as the multipart part name for
/// uploads (a fresh guid) and as the list key in the UI; the server resolves everything else by
/// reference (<see cref="ToRef"/>): drawings and progress photos from blob storage, forwards from
/// the original message, uploads from the request's file parts.
/// </summary>
public sealed record ComposeDraftAttachment(
    string Key,
    ComposeAttachmentSource Source,
    string Id,
    string FileName,
    long SizeBytes,
    string? SourceMessageId = null,
    IBrowserFile? File = null)
{
    public ComposeAttachmentRef ToRef() => new(
        Source,
        Source == ComposeAttachmentSource.Upload ? Key : Id,
        SourceMessageId,
        FileName);

    public static ComposeDraftAttachment FromUpload(IBrowserFile file) => new(
        Guid.NewGuid().ToString("N"), ComposeAttachmentSource.Upload, "", file.Name, file.Size, File: file);

    public static ComposeDraftAttachment FromDrawing(string drawingRevisionId, string fileName, long size) => new(
        Guid.NewGuid().ToString("N"), ComposeAttachmentSource.Drawing, drawingRevisionId, fileName, size);

    public static ComposeDraftAttachment FromProgressPhoto(string progressPhotoId, string fileName, long size) => new(
        Guid.NewGuid().ToString("N"), ComposeAttachmentSource.ProgressPhoto, progressPhotoId, fileName, size);

    public static ComposeDraftAttachment FromOriginal(string sourceMessageId, string attachmentId, string fileName, long size) => new(
        Guid.NewGuid().ToString("N"), ComposeAttachmentSource.OriginalMessage, attachmentId, fileName, size, sourceMessageId);
}
