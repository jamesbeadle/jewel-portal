namespace Jewel.JPMS.Models;

// One entry in a variation order's in-app conversation — the VO twin of RequestMessage's typed
// legs, without the mailbox columns: a variation's email correspondence stays in the live tagged
// mailbox, so this thread only ever holds messages typed in JPMS. Visibility reuses the shared
// MessageVisibility split: Internal notes never leave the staff view; Shared messages are the
// thread the client portal reads and writes.
public sealed record VariationOrderMessage(
    string MessageId,
    string VariationOrderId,
    string AuthorEmail,
    string AuthorName,
    string Body,
    MessageVisibility Visibility,
    DateTimeOffset PostedAt,
    // The message this one replies to; null for a top-level message. Replies nest freely.
    string? ParentMessageId = null);
