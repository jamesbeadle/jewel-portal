namespace Jewel.JPMS.Models;

/// <summary>
/// The outcome of drafting a work-order email in the shared mailbox: the order it covers, who the
/// draft is addressed to (the supplier's directory email), and where to open it. <see cref="WebLink"/>
/// opens the draft in Outlook on the web when Graph returns one (it usually does); null otherwise —
/// the draft is still in the mailbox's Drafts folder. Mirrors <see cref="BidPackageInviteDraft"/>.
/// </summary>
public sealed record WorkOrderEmailDraft(
    WorkOrder Order,
    string Subject,
    string RecipientEmail,
    string? WebLink);
