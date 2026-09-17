using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

public sealed partial class OutboundEmailDispatcher
{
    private const string SendRefusedNote =
        "The send didn't go through — the email is saved as a draft in the projects mailbox. "
        + "Open it in Outlook to send it from there, or try again here.";

    private async Task<OutboundEmailDispatch> LeftAsDraftAsync(
        MailboxDraftMessage message, OutboundEmailFiling filing, MailboxDraft draft, CancellationToken cancellationToken)
    {
        await RecordAsync(filing, AuditEventType.DraftCreated,
            $"Draft \"{message.Subject}\" staged for {Recipients(message)} — review and send from Outlook.",
            draft.Id, draft.WebLink, cancellationToken);
        return new OutboundEmailDispatch(draft.Id, draft.WebLink, Sent: false, FailureNote: null);
    }

    private async Task<OutboundEmailDispatch> SendRefusedAsync(
        MailboxDraftMessage message, OutboundEmailFiling filing, MailboxDraft draft, CancellationToken cancellationToken)
    {
        await RecordAsync(filing, AuditEventType.EmailSendFailed,
            $"Send failed for \"{message.Subject}\" (to {Recipients(message)}) — the email is saved in the mailbox's Drafts folder.",
            draft.Id, draft.WebLink, cancellationToken);
        return new OutboundEmailDispatch(draft.Id, draft.WebLink, Sent: false, SendRefusedNote);
    }

    /// <summary>Immutable ids: the draft's id stays valid on the sent message, so its webLink is
    /// re-read to point the audit row and the outcome at the sent copy.</summary>
    private async Task<OutboundEmailDispatch> SentAsync(
        MailboxDraftMessage message, OutboundEmailFiling filing, MailboxDraft draft, CancellationToken cancellationToken)
    {
        var sentWebLink = await mailbox.GetWebLinkAsync(draft.Id, cancellationToken) ?? draft.WebLink;
        await RecordAsync(filing, AuditEventType.EmailSent,
            $"Sent \"{message.Subject}\" to {Recipients(message)}.",
            draft.Id, sentWebLink, cancellationToken);
        return new OutboundEmailDispatch(draft.Id, sentWebLink, Sent: true, FailureNote: null);
    }

    private Task RecordAsync(
        OutboundEmailFiling filing, AuditEventType eventType, string detail,
        string messageId, string? webLink, CancellationToken cancellationToken) =>
        audit.WriteAsync(
            eventType,
            detail,
            pathway: filing.PathwayLabel,
            projectId: filing.ProjectId,
            recordType: filing.RecordType,
            recordId: filing.RecordId,
            recordReference: filing.RecordReference,
            emailMessageId: messageId,
            webLink: webLink,
            cancellationToken: cancellationToken);

    /// <summary>To and Cc, as the audit trail reads them — a Bcc never appears on a shared surface.</summary>
    private static string Recipients(MailboxDraftMessage message) =>
        string.Join(", ", message.To
            .Concat(message.Cc ?? Array.Empty<MailboxDraftRecipient>())
            .Select(recipient => recipient.Email)
            .Distinct(StringComparer.OrdinalIgnoreCase));
}
