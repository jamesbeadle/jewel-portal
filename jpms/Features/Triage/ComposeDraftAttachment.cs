using Jewel.JPMS.Contracts.MailboxCompose;

namespace Jewel.JPMS.Features.Triage;

/// <summary>
/// One attachment the composer is holding, before send. Key doubles as the multipart part name for
/// uploads (a fresh guid) and as the list key in the UI; the server resolves everything else by
/// reference (<see cref="ToRef"/>): drawings and progress photos from blob storage, forwards from
/// the original message, uploads from the request's file parts, record documents from the record's
/// own PDF renderer (rendered at send time, so the document is always current).
/// </summary>
public sealed record ComposeDraftAttachment(
    string Key,
    ComposeAttachmentSource Source,
    string Id,
    string FileName,
    long SizeBytes,
    string? SourceMessageId = null,
    IBrowserFile? File = null,
    RecordType? RecordType = null)
{
    public ComposeAttachmentRef ToRef() => new(
        Source,
        Source == ComposeAttachmentSource.Upload ? Key : Id,
        SourceMessageId,
        FileName,
        RecordType);

    public static ComposeDraftAttachment FromUpload(IBrowserFile file) => new(
        Guid.NewGuid().ToString("N"), ComposeAttachmentSource.Upload, "", file.Name, file.Size, File: file);

    public static ComposeDraftAttachment FromDrawing(string drawingRevisionId, string fileName, long size) => new(
        Guid.NewGuid().ToString("N"), ComposeAttachmentSource.Drawing, drawingRevisionId, fileName, size);

    public static ComposeDraftAttachment FromProgressPhoto(string progressPhotoId, string fileName, long size) => new(
        Guid.NewGuid().ToString("N"), ComposeAttachmentSource.ProgressPhoto, progressPhotoId, fileName, size);

    public static ComposeDraftAttachment FromOriginal(string sourceMessageId, string attachmentId, string fileName, long size) => new(
        Guid.NewGuid().ToString("N"), ComposeAttachmentSource.OriginalMessage, attachmentId, fileName, size, sourceMessageId);

    // Size is unknown client-side (the PDF is rendered at send time) — 0 renders as no size label,
    // which is honest. Request-family documents are small; the 25 MB planner still guards the total.
    public static ComposeDraftAttachment FromRecordDocument(LinkableRecord record) => new(
        Guid.NewGuid().ToString("N"), ComposeAttachmentSource.RecordDocument, record.RecordId,
        $"{record.Reference}.pdf", 0, RecordType: record.Type);
}

/// <summary>The starting envelope for a brand-new email composed from a record page — who it
/// goes to, what it says, what travels with it. Plain-text body (the composer makes paragraphs).</summary>
public sealed record ComposePrefill(
    string To = "",
    string Subject = "",
    string Body = "",
    IReadOnlyList<ComposeDraftAttachment>? Attachments = null);
