namespace Jewel.JPMS.Models;

/// <summary>
/// What became of a request's official document on its way out: where it went and who it is
/// addressed to. <see cref="WebLink"/> opens the message in Outlook on the web when Graph returns
/// one (it usually does).
/// Cc/Bcc list the copied recipients the draft carries; showing Bcc here is correct because the
/// person reviewing the draft is internal (Bcc stays off every client-facing surface).
/// <see cref="DraftMessageId"/> is the staged draft's mailbox message id — the handle for
/// withdrawing the draft (DeleteMailboxDraft) if it was staged in error; null only on legacy
/// payloads.
/// </summary>
public sealed record RequestEmailOutcome(
    string RequestId,
    string Subject,
    IReadOnlyList<string> Recipients,
    string? WebLink,
    IReadOnlyList<string>? Cc = null,
    IReadOnlyList<string>? Bcc = null,
    string? DraftMessageId = null,
    // Sent=true means the correspondent has the document. Sent=false is a draft waiting in the
    // mailbox's Drafts folder — by choice when FailureNote is null, because the send was refused
    // when it is not.
    bool Sent = false,
    string? FailureNote = null);
