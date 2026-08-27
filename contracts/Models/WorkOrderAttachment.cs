namespace Jewel.JPMS.Models;

/// <summary>Where a work-order attachment came from.</summary>
public enum WorkOrderAttachmentSource
{
    /// <summary>A file uploaded from the computer (the Add/Edit order form or the PO page).</summary>
    Upload = 0,
    /// <summary>Copied off the triaged email the order was raised from (Control Centre).</summary>
    Email = 1,
    /// <summary>Copied off the assistant conversation the order was drafted from — the quote the
    /// user attached to the chat, kept on the order without being re-picked from disk.</summary>
    Chat = 2
}

/// <summary>
/// A file kept on a work order for record keeping — the quote the order was raised against, a
/// signed paper copy, a photo of the scope. Pure paperwork for the office: attachments are NEVER
/// sent to the supplier — they don't travel with the purchase-order email and they don't print on
/// the PO. Files live in their own private container and are proxied on download.
/// </summary>
public sealed record WorkOrderAttachment(
    string WorkOrderAttachmentId,
    string WorkOrderId,
    string ProjectId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    WorkOrderAttachmentSource Source,
    DateTimeOffset AddedAt,
    string AddedByEmail)
{
    /// <summary>True for the image types a browser can show inline, so lists can thumbnail them.</summary>
    public bool IsImage =>
        ContentType is { Length: > 0 } type
        && type.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>The label a person reads — the file's name, with a safe fallback.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(FileName) ? "Attachment" : FileName;
}
