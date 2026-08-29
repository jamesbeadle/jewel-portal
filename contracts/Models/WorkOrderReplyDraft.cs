namespace Jewel.JPMS.Models;

/// <summary>
/// The outcome of preparing a threaded reply draft carrying a work order's purchase-order PDF
/// (PrepareWorkOrderReplyDraft): where the draft went and who Graph pre-filled it to (reply-all —
/// the original conversation's participants, which is the point of a reply). <see cref="WebLink"/>
/// opens the draft in Outlook on the web when Graph returns one (it usually does); null otherwise —
/// the draft is still in the projects mailbox's Drafts folder. Mirrors RequestEmailDraft's shape.
/// </summary>
public sealed record WorkOrderReplyDraft(
    string WorkOrderId,
    string Reference,
    string Subject,
    IReadOnlyList<string> To,
    IReadOnlyList<string> Cc,
    string? WebLink);
