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

// The attachments carried by ONE message of an email thread, for the composer's "from this
// thread" picker: a reply often needs a file that arrived two messages back and has not been
// through document triage yet. Only messages with at least one real (non-inline) attachment are
// listed. MessageId is the Graph id the attachment bytes are read back by at send time
// (ComposeAttachmentSource.OriginalMessage + SourceMessageId).
public sealed record ConversationAttachmentGroup(
    string MessageId,
    string FromName,
    string FromEmail,
    string Subject,
    DateTimeOffset ReceivedAt,
    IReadOnlyList<IntakeAttachment> Attachments);
