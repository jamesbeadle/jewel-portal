using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

/// <summary>
/// Creates an Outlook draft REPLY to an email already linked to the work order, carrying the
/// freshly rendered purchase-order PDF. The reply stays in the original conversation thread —
/// "RE:" subject, quoted history, original recipients (reply-all) — so the formal purchase order
/// lands inside the email chain the works were agreed in, instead of starting a fresh thread.
/// <see cref="MailboxMessageId"/> is the Graph id of the conversation email to reply to (from the
/// order's tagged correspondence); <see cref="HtmlCoverNote"/> is placed above the quoted history,
/// composed by the caller like every other work-order email body. Nothing is sent — the draft sits
/// in the projects mailbox's Drafts folder until a person reviews and sends it from Outlook — and,
/// unlike the request flow, drafting never moves the order's status: a work order's lifecycle is
/// driven by approval/acceptance, not by its covering email.
/// </summary>
public sealed record PrepareWorkOrderReplyDraft(
    string WorkOrderId,
    string MailboxMessageId,
    string HtmlCoverNote) : ICommand<WorkOrderReplyDraft>;
