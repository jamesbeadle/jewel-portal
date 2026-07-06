namespace Jewel.JPMS.Models;

// One attachment on a mailbox message. Size is in bytes; ContentType is the MIME type when Graph
// reports it. We surface names only — files are not downloaded into JPMS. Shared by the live-read
// triage detail (MailboxMessageDetail).
public sealed record IntakeAttachment(
    string Name,
    long Size,
    string? ContentType,
    // Graph attachment id — lets the triage UI act on a specific attachment (e.g. save it into a
    // project's drawings). Empty for legacy snapshots that never recorded ids.
    string Id = "");
