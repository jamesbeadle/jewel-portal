namespace Jewel.JPMS.Models;

/// <summary>What kind of thing is attached to a request.</summary>
public enum RequestAttachmentKind
{
    /// <summary>A pointer at a revision already in the project's drawing register.</summary>
    Drawing = 0,
    /// <summary>A file uploaded straight onto the request — usually a site photo.</summary>
    File = 1
}

/// <summary>
/// Something attached to a request so the query can be understood without hunting for context: a
/// drawing revision from the project register, or a photograph taken on site.
///
/// The two kinds are deliberately one list rather than two. A site manager standing in front of the
/// problem photographs it and points at the detail it contradicts; whoever reads the RFI wants both
/// together, in the order they were added, not split across two panels.
///
/// Drawings are LINKED, never copied: the register stays the single source of truth for what
/// revision is current, so an RFI can never quietly carry a superseded drawing that has since been
/// re-issued. Uploaded files are stored in their own private container and proxied on download.
/// </summary>
public sealed record RequestAttachment(
    string RequestAttachmentId,
    string RequestId,
    string ProjectId,
    RequestAttachmentKind Kind,
    // Drawing links: which revision, plus the register's own labels denormalised so the list
    // renders without a join and still reads correctly if the drawing is later deleted.
    string? DrawingId,
    string? DrawingRevisionId,
    string? DrawingCode,
    string? RevisionLabel,
    // Uploaded files.
    string? FileName,
    string? ContentType,
    long? FileSizeBytes,
    // What the person adding it wanted to say about it ("cill detail as built, 14 Mar").
    string? Caption,
    DateTimeOffset AddedAt,
    string AddedByEmail)
{
    /// <summary>True for the image types a browser can show inline, so the list can thumbnail them.</summary>
    public bool IsImage =>
        Kind == RequestAttachmentKind.File
        && ContentType is { Length: > 0 } type
        && type.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>The label a person reads: the drawing's code and revision, or the file's name.</summary>
    public string DisplayName => Kind switch
    {
        RequestAttachmentKind.Drawing =>
            string.IsNullOrWhiteSpace(RevisionLabel)
                ? (DrawingCode ?? "Drawing")
                : $"{DrawingCode} rev {RevisionLabel}",
        _ => string.IsNullOrWhiteSpace(FileName) ? "Attachment" : FileName!
    };
}
