namespace Jewel.JPMS.Models;

/// <summary>
/// What happened to an automatically sent purchase-order email (SendWorkOrderPoEmail).
/// Sent=true means the supplier has the email; Sent=false means the send failed AFTER the draft
/// was staged — the draft survives in the mailbox's Drafts folder (open it via <see cref="WebLink"/>)
/// and <see cref="FailureNote"/> says so in words the UI can show. The work order itself is
/// unaffected either way. Mirrors ComposeOutcome's send-degrades-to-draft shape.
/// </summary>
public sealed record WorkOrderPoEmailOutcome(
    string WorkOrderId,
    bool Sent,
    string RecipientEmail,
    string? WebLink,
    string? FailureNote = null);
