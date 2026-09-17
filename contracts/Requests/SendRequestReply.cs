using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// Emails the request's official document (the RFI PDF) as a REPLY to an email already linked to
/// it. The reply stays in the original conversation thread — "RE:" subject, quoted history,
/// original recipients (reply-all) — so the formal document lands inside the email chain the
/// discussion started in, signalling the official process without splitting the correspondence.
/// <see cref="MailboxMessageId"/> is the Graph id of the conversation email to reply to.
///
/// SaveAsDraftOnly stops after staging, leaving the reviewed draft in the mailbox's Drafts folder
/// for Outlook — what this command always did before 2026-09-17, now a choice rather than a fate.
/// A failed send degrades to exactly that and says so, so the document is never lost.
/// </summary>
public sealed record SendRequestReply(
    string RequestId,
    string MailboxMessageId,
    bool SaveAsDraftOnly = false) : ICommand<RequestEmailOutcome>;
