namespace Jewel.JPMS.Models;

// Lifecycle of an email that has landed in the requests@ mailbox. Every ingested email is
// recorded exactly once and starts at NeedsTriage; a human then either links it to an
// existing request, creates a new request from it, or discards it. Nothing is ever silently
// dropped — an email that can't be auto-handled simply waits in the triage queue.
public enum IntakeStatus
{
    NeedsTriage = 0, // waiting for a human to deal with it
    Claimed = 1,     // a staff member has taken ownership to work it
    Linked = 2,      // attached to a request (existing or newly created)
    Discarded = 3,   // spam / not relevant
    Failed = 4       // processing error; needs attention
}

// One inbound email captured from the shared projects@ mailbox, awaiting or having
// completed triage. InternetMessageId is the mailbox's stable per-message identifier and is
// what we dedupe on so the same email is never recorded twice. The threading identifiers
// (ConversationId / InReplyTo / ReferencesHeader) are kept so triage can suggest the request
// an email most likely belongs to. GraphMessageId is the mailbox item's current Graph id —
// used to move the email into an outcome folder as triage progresses; it changes whenever the
// message is moved, so the ingestion layer refreshes it after each move.
public sealed record IntakeEmail(
    string IntakeId,
    string InternetMessageId,
    string? ConversationId,
    string? InReplyTo,
    string? ReferencesHeader,
    string FromEmail,
    string FromName,
    string Subject,
    string BodyPreview,
    bool HasAttachments,
    DateTimeOffset ReceivedAt,
    IntakeStatus Status,
    string? ClaimedByEmail,
    DateTimeOffset? ClaimedAt,
    string? LinkedRequestId,
    string? Notes,
    string? GraphMessageId = null);
