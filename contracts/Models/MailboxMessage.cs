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
// metadata), fetched live when a triager opens it. Keyed by the live message id. The envelope
// fields (From/To/Cc/ReplyTo/Subject) feed the composer's reply prefill — the UI computes the
// reply-all set for display, and the server re-applies whatever the user finally submits. All
// optional so existing callers and cached JSON stay compatible.
public sealed record MailboxMessageDetail(
    string MessageId,
    string BodyHtml,
    bool BodyIsHtml,
    IReadOnlyList<IntakeAttachment> Attachments,
    string? FromEmail = null,
    string? FromName = null,
    IReadOnlyList<string>? To = null,
    IReadOnlyList<string>? Cc = null,
    string? ReplyTo = null,
    string? Subject = null,
    // The shared projects mailbox address. The composer filters it out of the reply-all prefill —
    // the server auto-Cc's it on every send, so showing it in the visible Cc is pure noise.
    string? MailboxAddress = null,
    // The email's JPMS tags and pathway bucket AS THEY ARE NOW — the same split as MailboxMessage's
    // Categories/Bucket, but read live from the message itself rather than the list page it was
    // picked from. A list row goes stale the moment something tags the email while it stays open
    // (System Actions' Create now raising a record from it); the Control Centre reconciles the
    // open email against these. Null when the read couldn't reach the mailbox.
    IReadOnlyList<string>? Categories = null,
    string? Bucket = null);

// One page of a live, server-side-filtered mailbox read. Graph pages these with an opaque cursor
// (its own nextLink) rather than an offset, so NextCursor — when non-null — is passed straight back
// to fetch the next page. Total is the count of all messages matching the filter (the whole pile).
public sealed record MailboxPage(
    IReadOnlyList<MailboxMessage> Items,
    string? NextCursor,
    int Total,
    // Thread reads only: true when the members were found by subject rather than by Graph's
    // ConversationId (the id splits when a subject is edited or a forward re-threads), so the UI
    // can say the grouping is a best match rather than Outlook's own.
    bool MatchedBySubject = false);
