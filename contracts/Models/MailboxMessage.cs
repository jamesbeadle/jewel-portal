namespace Jewel.JPMS.Models;

// A single message as read live from a mailbox folder (Inbox = triage queue, General = discarded).
// Nothing here is persisted: in the live-read model the mailbox is the source of truth and each
// triage view is a fresh read of a folder. Id is the message's current Graph id, used to act on it
// (move to a folder); InternetMessageId is the stable id used to re-find the message if its Graph id
// has changed since the list was rendered.
public sealed record MailboxMessage(
    string Id,
    string InternetMessageId,
    string FromEmail,
    string FromName,
    string Subject,
    string BodyPreview,
    bool HasAttachments,
    DateTimeOffset ReceivedAt);

// The full, on-demand content of one mailbox message (sanitised HTML body + non-inline attachment
// metadata), fetched live when a triager opens it. Keyed by the live message id.
public sealed record MailboxMessageDetail(
    string MessageId,
    string BodyHtml,
    bool BodyIsHtml,
    IReadOnlyList<IntakeAttachment> Attachments);

// One page of a live, server-side-filtered mailbox read. Graph pages these with an opaque cursor
// (skiptoken) rather than an offset, so NextCursor — when non-null — is passed back to fetch the
// next page. Total is the count of all messages matching the filter (the whole queue / pile size).
public sealed record MailboxPage(
    IReadOnlyList<MailboxMessage> Items,
    string? NextCursor,
    int Total);
