namespace Jewel.JPMS.Models;

// The full, on-demand content of a single intake email, fetched live from Microsoft Graph when a
// triager opens it — deliberately NOT stored in the database (only the short BodyPreview and a
// HasAttachments flag are persisted). BodyHtml is the original HTML body after server-side
// sanitisation, safe to render. Attachments lists the real (non-inline) attachment metadata so the
// triager can see what came with the email. When Graph is unavailable or the email has no Graph
// handle, BodyHtml falls back to the stored preview and Attachments is empty.
public sealed record IntakeEmailDetail(
    string IntakeId,
    string BodyHtml,
    bool BodyIsHtml,
    IReadOnlyList<IntakeAttachment> Attachments);

// One attachment on an intake email. Size is in bytes; ContentType is the MIME type when Graph
// reports it. We surface names only — files are not downloaded into JPMS.
public sealed record IntakeAttachment(
    string Name,
    long Size,
    string? ContentType);
