using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// Creates an Outlook draft REPLY to an email already linked to the request, carrying the request's
/// official document (the RFI PDF). The reply stays in the original conversation thread — "RE:"
/// subject, quoted history, original recipients (reply-all) — so the formal document lands inside
/// the email chain the discussion started in, signalling the official process without splitting the
/// correspondence. <see cref="MailboxMessageId"/> is the Graph id of the inbound conversation email
/// to reply to. Nothing is sent — the draft sits in the projects mailbox's Drafts folder until a
/// person reviews and sends it from Outlook.
/// </summary>
public sealed record PrepareRequestReplyDraft(
    string RequestId,
    string MailboxMessageId) : ICommand<RequestEmailDraft>;
