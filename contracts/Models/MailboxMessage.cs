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
    DateTimeOffset ReceivedAt,
    // The JPMS workflow tags on this email (e.g. "JPMS/Discarded", "JPMS/RFI-001"), shown as chips on
    // the Tagged tab. Excludes the bare "JPMS" marker (internal). Empty for untagged queue messages.
    IReadOnlyList<string> Categories,
    // Graph's thread grouping id: the email plus every reply/forward of it share one ConversationId.
    // Used to read the whole thread when a message is opened in triage, so later replies (which often
    // say how the older messages should be triaged) are visible alongside. Empty when Graph omits it.
    string ConversationId = "",
    // The communication pathway this thread is filed under — the exact bucket category
    // ("JPMS/Client", "JPMS/Subcontractor" or "JPMS/Internal"), or null when the thread has no
    // pathway yet. Derived server-side from the message's categories (which exclude it, so bucket
    // tags never render as ordinary chips) — clients read this field, never parse tag strings.
    string? Bucket = null,
    // Record tags carried by OTHER messages in this email's conversation (e.g. "JPMS/REQ-0007") —
    // set only on triage-queue reads, and only when the thread was already triaged. This message
    // itself is still untagged and still needs its own triage decision; the UI shows these as a
    // "reply to an already-linked thread" hint so re-linking is one step. Null elsewhere.
    IReadOnlyList<string>? ThreadTags = null);

// The full, on-demand content of one mailbox message (sanitised HTML body + non-inline attachment
// metadata), fetched live when a triager opens it. Keyed by the live message id.
public sealed record MailboxMessageDetail(
    string MessageId,
    string BodyHtml,
    bool BodyIsHtml,
    IReadOnlyList<IntakeAttachment> Attachments);

// One page of a live, server-side-filtered mailbox read. Graph pages these with an opaque cursor
// (its own nextLink) rather than an offset, so NextCursor — when non-null — is passed straight back
// to fetch the next page. Total is the count of all messages matching the filter (the whole pile).
public sealed record MailboxPage(
    IReadOnlyList<MailboxMessage> Items,
    string? NextCursor,
    int Total);
