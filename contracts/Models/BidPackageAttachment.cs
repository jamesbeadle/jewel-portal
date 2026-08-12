namespace Jewel.JPMS.Models;

/// <summary>Where a bid-package attachment came from.</summary>
public enum BidPackageAttachmentSource
{
    /// <summary>A file uploaded from the computer (the package's Documents section).</summary>
    Upload = 0,
    /// <summary>Copied off a triaged email (reserved — not produced yet).</summary>
    Email = 1
}

/// <summary>
/// A file kept on a bid package as part of its tender documents — a specification extract, a
/// schedule of finishes, a survey photo: anything a tenderer needs that isn't a drawing in the
/// project's Drawings register. UNLIKE work-order attachments these are supplier-facing: they are
/// attached to the tender-invite email alongside the linked drawings (oversized files travel as
/// 7-day download links, same planner). Files live in their own private container and are proxied
/// on download.
/// </summary>
public sealed record BidPackageAttachment(
    string BidPackageAttachmentId,
    string BidPackageId,
    string ProjectId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    BidPackageAttachmentSource Source,
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
