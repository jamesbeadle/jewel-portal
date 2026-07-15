namespace Jewel.JPMS.Models;

// Who a conversation message is visible to. Mirrors the InternalNotes / ClientNotes
// split on the Request itself: a request thread mixes internal Jewel discussion with
// messages shared out to external participants (architect, subcontractor, client).
public enum MessageVisibility
{
    Internal = 0, // Jewel staff only
    Shared = 1    // visible to external participants on the request
}

// Where a conversation message came from / is going. Manually typed in-app messages are
// System. Inbound messages were ingested from the requests@ mailbox; Outbound messages
// are shared replies that were (or will be) emailed back out via Graph.
public enum MessageDirection
{
    System = 0,   // typed in JPMS, no email leg
    Inbound = 1,  // arrived from the mailbox
    Outbound = 2  // sent (or queued to send) out by email
}

// Delivery state for the outbound email leg of a Shared message. Internal/System messages
// stay NotApplicable; an Outbound message moves Pending -> Sent or Pending -> Failed.
public enum MessageSentStatus
{
    NotApplicable = 0,
    Pending = 1,
    Sent = 2,
    Failed = 3
}

// A single entry in a request's back-and-forth conversation. Requests are long-running
// discussions, so every contribution is captured here with its author and timestamp,
// giving the auditable thread that replaces the email chains RFIs are run on today.
// The trailing fields carry the email/threading metadata used by the mailbox automation:
// they are null/default for ordinary in-app messages.
public sealed record RequestMessage(
    string MessageId,
    string RequestId,
    string AuthorEmail,
    string AuthorName,
    string Body,
    MessageVisibility Visibility,
    DateTimeOffset PostedAt,
    MessageDirection Direction = MessageDirection.System,
    string? EmailMessageId = null,
    string? InReplyTo = null,
    string? ConversationId = null,
    MessageSentStatus SentStatus = MessageSentStatus.NotApplicable,
    // The live Graph mailbox id of an Inbound message (null for in-app/outbound legs). Lets the
    // conversation view fetch the email's FULL body + attachments on demand (the listed Body is only
    // Graph's short bodyPreview snippet, which truncates long emails and drops the quoted thread).
    string? MailboxId = null,
    // The email's subject line (Inbound messages only; null for in-app messages). Lets pickers
    // present a tagged email chain by the name its correspondents know it by.
    string? Subject = null);
