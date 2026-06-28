namespace Jewel.JPMS.Api.Features.MailboxIntake.Queue;

/// <summary>Storage queue names used by the mailbox-intake feature.</summary>
public static class MailboxQueues
{
    /// <summary>Webhook notifications to fetch + ingest (carries a Graph message id).</summary>
    public const string IntakeNotifications = "mailbox-intake-notifications";

    /// <summary>Mailbox side-effects driven by triage (folder moves, outbound sends).</summary>
    public const string MailboxActions = "mailbox-actions";
}

/// <summary>
/// A mailbox side-effect to perform out-of-band so it can retry without blocking the triage action.
/// </summary>
public sealed record MailboxActionMessage(
    MailboxActionType Type,
    string IntakeId,
    // For moves: the triage outcome whose folder we move into. For sends: ignored.
    int? TargetStatus = null,
    // For outbound sends only.
    string? RequestMessageId = null,
    // For SendRequestDocument only: the request whose document to render + email.
    string? RequestId = null,
    // For SendRequestDocument only: when set, the document is emailed to this single address
    // (an ad-hoc resend) instead of the project's flagged contacts.
    string? RecipientOverride = null);

public enum MailboxActionType
{
    MoveToOutcomeFolder = 0,
    SendOutbound = 1,
    // Move an email back to the Inbox so it re-enters the triage queue (used by return-to-triage undo).
    ReturnToInbox = 2,
    // Render a request's document (RFI etc.) from SQL and email it to the project's contacts.
    SendRequestDocument = 3
}
