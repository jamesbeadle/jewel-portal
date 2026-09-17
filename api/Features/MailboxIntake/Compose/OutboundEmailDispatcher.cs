using Jewel.JPMS.Api.Features.Audit;
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
/// </summary>
public sealed partial class OutboundEmailDispatcher
{
    private readonly IMailboxGraphClient mailbox;
    private readonly AuditTrail audit;

    public OutboundEmailDispatcher(IMailboxGraphClient mailbox, AuditTrail audit)
    {
        this.mailbox = mailbox;
        this.audit = audit;
    }

    public async Task<OutboundEmailDispatch> DispatchAsync(
        MailboxDraftMessage message,
        OutboundEmailFiling filing,
        bool saveAsDraftOnly,
        CancellationToken cancellationToken)
    {
        var draft = await mailbox.CreateDraftAsync(message, cancellationToken)
            ?? throw new InvalidOperationException(filing.StagingRefusal);

        if (saveAsDraftOnly) return await LeftAsDraftAsync(message, filing, draft, cancellationToken);
        if (await mailbox.SendDraftAsync(draft.Id, cancellationToken))
            return await SentAsync(message, filing, draft, cancellationToken);
        return await SendRefusedAsync(message, filing, draft, cancellationToken);
    }
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
/// </summary>
public sealed record OutboundEmailDispatch(
    string MessageId,
    string? WebLink,
    bool Sent,
    string? FailureNote);
