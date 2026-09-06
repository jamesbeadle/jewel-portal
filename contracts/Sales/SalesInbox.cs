using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Sales;

/// <summary>
/// The sales inbox (2026-09-06): sales@jewelbb.co.uk read live from Microsoft Graph, its own thing
/// under the Sales folder — deliberately NOT the Control Centre's triage. Nothing is tagged, moved
/// or stored: the list is the mailbox's Inbox as it stands, each sender matched to the lead whose
/// contact email it is, so the conversation with a prospect and the record of it sit side by side.
/// Replies go out from sales@ itself (reply-all draft, then sent), and an email can be logged on a
/// lead's timeline in one click.
/// </summary>
public sealed record ListSalesInbox(string? Cursor = null, int Take = 25, bool NewestFirst = true, string? Search = null) : IQuery<SalesInboxPage>;

/// <summary>Every message in one Graph conversation from the sales mailbox, oldest first.</summary>
public sealed record GetSalesInboxConversation(string ConversationId) : IQuery<MailboxPage>;

/// <summary>One sales email's full (sanitised) body and attachment names, read live.</summary>
public sealed record GetSalesInboxMessage(string MessageId) : IQuery<MailboxMessageDetail>;

/// <summary>One page of the sales inbox, with the leads its senders match (by contact email) so
/// the page can chip each row with LD-#### and open it. Configured is false when the API has no
/// Graph credentials — the page then says so instead of showing an empty inbox.</summary>
public sealed record SalesInboxPage(
    MailboxPage Page,
    IReadOnlyList<SalesInboxLeadMatch> Matches,
    string MailboxAddress,
    bool Configured,
    string? Notice = null);

/// <summary>A sender's email → the lead whose contact email it is.</summary>
public sealed record SalesInboxLeadMatch(string Email, string LeadId, string Reference, string ContactName, LeadStage Stage);

/// <summary>
/// Reply to a sales email from sales@ — a reply-all draft is staged with the body above the quoted
/// history and sent in the same call. When Graph refuses the send (Mail.Send not consented, or
/// outbound send disabled) the draft is left in the mailbox's Drafts for a person to send from
/// Outlook, and the outcome says so. Optionally logs the reply on a lead's timeline.
/// SentByEmail is stamped by the server.
/// </summary>
public sealed record ReplyToSalesEmail(string MessageId, string Body, string? LeadId, string SentByEmail = "") : ICommand<SalesReplyOutcome>;

public sealed record SalesReplyOutcome(bool Sent, string? DraftWebLink, string Message);

/// <summary>Records a sales email (its subject and sender) as an Email activity on a lead's
/// timeline — how a conversation gets onto the record without copying the mail anywhere.
/// RecordedByEmail is stamped by the server.</summary>
public sealed record LogSalesEmailToLead(string MessageId, string LeadId, string? Note, string RecordedByEmail = "") : ICommand<LeadActivity>;
