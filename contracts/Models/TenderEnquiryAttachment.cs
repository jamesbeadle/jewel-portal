namespace Jewel.JPMS.Models;

/// <summary>Where a tender-enquiry attachment came from.</summary>
public enum TenderEnquiryAttachmentSource
{
    /// <summary>A file uploaded from the computer (the enquiry's Documents section).</summary>
    Upload = 0,
    /// <summary>Copied off the architect's email when the enquiry was logged — the PQQ, the drawings.</summary>
    Email = 1
}

/// <summary>
/// A file kept on a tender enquiry — the questionnaire as received, the drawings that came with
/// it, Jewel's supporting material. Files live in their own private container and are proxied on
/// download, the same arrangement as bid-package attachments.
/// </summary>
public sealed record TenderEnquiryAttachment(
    string TenderEnquiryAttachmentId,
    string TenderEnquiryId,
    string ProjectId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    TenderEnquiryAttachmentSource Source,
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
