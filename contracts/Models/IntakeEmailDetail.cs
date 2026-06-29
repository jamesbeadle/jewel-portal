namespace Jewel.JPMS.Models;

// One attachment on a mailbox message. Size is in bytes; ContentType is the MIME type when Graph
// reports it. We surface names only — files are not downloaded into JPMS. Shared by the live-read
// triage detail (MailboxMessageDetail).
public sealed record IntakeAttachment(
    string Name,
    long Size,
    string? ContentType);
