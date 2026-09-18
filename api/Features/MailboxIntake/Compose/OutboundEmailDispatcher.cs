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
        var draft = await mailbox.CreateDraftAsync(message, cancellationToken)
            ?? throw new InvalidOperationException(filing.StagingRefusal);
        var staged = new StagedEmail(
            draft.Id, draft.WebLink, message.Subject, Addresses(message.To), Addresses(message.Cc));
        return await FinishAsync(staged, filing, saveAsDraftOnly, cancellationToken);
    }

    public async Task<OutboundEmailDispatch> DispatchReplyAsync(
        MailboxReplyDraftMessage reply,
        OutboundEmailFiling filing,
        bool saveAsDraftOnly,
        CancellationToken cancellationToken)
    {
        var draft = await mailbox.CreateReplyDraftAsync(reply, cancellationToken)
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

/// <summary>
/// Where a dispatched email belongs: the pathway its thread is born on, the record it is filed
/// under, and the sentence the person reads when the mailbox will not take the draft at all —
/// written at the door they pressed, because only that door knows what they should do instead.
/// </summary>
public sealed record OutboundEmailFiling(
    string PathwayLabel,
    string StagingRefusal,
    string? ProjectId = null,
    RecordType? RecordType = null,
    string? RecordId = null,
    string RecordReference = "");

/// <summary>
/// What became of it. Sent=false with no FailureNote is a draft left in the mailbox by choice;
/// Sent=false with one is a send the mailbox refused, and the draft is still there to finish.
/// Subject, To and Cc are the envelope as it actually went — which for a reply is Graph's, not the
/// caller's, so a caller reports what the correspondent will see rather than what it asked for.
/// </summary>
public sealed record OutboundEmailDispatch(
    string MessageId,
    string? WebLink,
    bool Sent,
    string? FailureNote,
    string Subject = "",
    IReadOnlyList<string>? To = null,
    IReadOnlyList<string>? Cc = null);
