using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

/// <summary>
/// The one way a record's email leaves the portal: stage the message as a draft in the projects
/// mailbox, send it, and say on the audit trail what happened to it.
///
/// Every record's outbound email used to carry its own copy of this sequence and they drifted —
/// some sent, some only drafted, some audited, some leaving no trace at all. The ordering is the
/// one triage compose keeps (SendMailboxEmailHandler), and it is why a portal email cannot be
/// lost: staging comes first and the send comes last, so a refused send leaves the reviewed draft
/// in the mailbox's Drafts folder with a note saying to finish it in Outlook, and the record the
/// email belongs to is never touched.
///
/// Every body it stages goes through ComposeHtmlPipeline.FromPortalDocument first, so no door
/// can send a script, an event handler or a javascript: link however carelessly it composed — the
/// branding survives, because that rule is the one written for mail the portal wrote itself.
///
/// Sales replies leave from a second mailbox, so the sequence is not tied to one address: the
/// dispatcher is told which mailbox it stages into and names it in the sentence a person reads
/// when a send is refused.
///
/// A record's document goes out either as a new thread (<see cref="DispatchAsync"/>) or inside an
/// email conversation it already has (<see cref="DispatchReplyAsync"/>, where Graph supplies the
/// threading headers, the recipients and the quoted history). Both end the same way, so the door a
/// person pressed never decides whether their email was sent.
/// </summary>
public sealed partial class OutboundEmailDispatcher
{
    private readonly IMailboxGraphClient mailbox;
    private readonly Audit.AuditTrail audit;
    private readonly string mailboxName;

    public OutboundEmailDispatcher(
        IMailboxGraphClient mailbox, Audit.AuditTrail audit, string mailboxName = "the projects mailbox")
    {
        this.mailbox = mailbox;
        this.audit = audit;
        this.mailboxName = mailboxName;
    }

    public async Task<OutboundEmailDispatch> DispatchAsync(
        MailboxDraftMessage message,
        OutboundEmailFiling filing,
        bool saveAsDraftOnly,
        CancellationToken cancellationToken)
    {
        var cleaned = message with { HtmlBody = ComposeHtmlPipeline.FromPortalDocument(message.HtmlBody) };
        var draft = await mailbox.CreateDraftAsync(cleaned, cancellationToken)
            ?? throw new InvalidOperationException(filing.StagingRefusal);
        var staged = new StagedEmail(
            draft.Id, draft.WebLink, cleaned.Subject, Addresses(cleaned.To), Addresses(cleaned.Cc));
        return await FinishAsync(staged, filing, saveAsDraftOnly, cancellationToken);
    }

    public async Task<OutboundEmailDispatch> DispatchReplyAsync(
        MailboxReplyDraftMessage reply,
        OutboundEmailFiling filing,
        bool saveAsDraftOnly,
        CancellationToken cancellationToken)
    {
        var cleaned = reply with { HtmlCoverNote = ComposeHtmlPipeline.FromPortalDocument(reply.HtmlCoverNote) };
        var draft = await mailbox.CreateReplyDraftAsync(cleaned, cancellationToken)
            ?? throw new InvalidOperationException(filing.StagingRefusal);
        var staged = new StagedEmail(draft.Id, draft.WebLink, draft.Subject, draft.To, draft.Cc);
        return await FinishAsync(staged, filing, saveAsDraftOnly, cancellationToken);
    }

    /// <summary>A message now sitting in Drafts, with the envelope the caller reports and the
    /// audit trail names it by. To and Cc only — a Bcc never appears on a shared surface.</summary>
    private sealed record StagedEmail(
        string Id, string? WebLink, string Subject, IReadOnlyList<string> To, IReadOnlyList<string> Cc)
    {
        public string Recipients =>
            string.Join(", ", To.Concat(Cc).Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> Addresses(IReadOnlyList<MailboxDraftRecipient>? recipients) =>
        (recipients ?? Array.Empty<MailboxDraftRecipient>()).Select(recipient => recipient.Email).ToList();
}
