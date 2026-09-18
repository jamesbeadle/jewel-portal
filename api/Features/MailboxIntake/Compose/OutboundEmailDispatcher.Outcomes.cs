using Jewel.JPMS.Api.Features.Audit;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

public sealed partial class OutboundEmailDispatcher
{
    private string SendRefusedNote =>
        $"The send didn't go through — the email is saved as a draft in {mailboxName}. "
        + "Open it in Outlook to send it from there, or try again here.";

    private async Task<OutboundEmailDispatch> FinishAsync(
        StagedEmail staged, OutboundEmailFiling filing, bool saveAsDraftOnly, CancellationToken cancellationToken)
    {
        if (saveAsDraftOnly) return await LeftAsDraftAsync(staged, filing, cancellationToken);
        if (await mailbox.SendDraftAsync(staged.Id, cancellationToken))
            return await SentAsync(staged, filing, cancellationToken);
        return await SendRefusedAsync(staged, filing, cancellationToken);
    }

    private async Task<OutboundEmailDispatch> LeftAsDraftAsync(
        StagedEmail staged, OutboundEmailFiling filing, CancellationToken cancellationToken)
    {
        await RecordAsync(filing, AuditEventType.DraftCreated,
            $"Draft \"{staged.Subject}\" staged for {staged.Recipients} — review and send from Outlook.",
            staged, staged.WebLink, cancellationToken);
        return Dispatched(staged, staged.WebLink, sent: false, failureNote: null);
    }

    private async Task<OutboundEmailDispatch> SendRefusedAsync(
        StagedEmail staged, OutboundEmailFiling filing, CancellationToken cancellationToken)
    {
        await RecordAsync(filing, AuditEventType.EmailSendFailed,
            $"Send failed for \"{staged.Subject}\" (to {staged.Recipients}) — the email is saved in the mailbox's Drafts folder.",
            staged, staged.WebLink, cancellationToken);
        return Dispatched(staged, staged.WebLink, sent: false, SendRefusedNote);
    }

    /// <summary>Immutable ids: the draft's id stays valid on the sent message, so its webLink is
    /// re-read to point the audit row and the outcome at the sent copy.</summary>
    private async Task<OutboundEmailDispatch> SentAsync(
        StagedEmail staged, OutboundEmailFiling filing, CancellationToken cancellationToken)
    {
        var sentWebLink = await mailbox.GetWebLinkAsync(staged.Id, cancellationToken) ?? staged.WebLink;
        await RecordAsync(filing, AuditEventType.EmailSent,
            $"Sent \"{staged.Subject}\" to {staged.Recipients}.", staged, sentWebLink, cancellationToken);
        return Dispatched(staged, sentWebLink, sent: true, failureNote: null);
    }

    private static OutboundEmailDispatch Dispatched(
        StagedEmail staged, string? webLink, bool sent, string? failureNote) =>
        new(staged.Id, webLink, sent, failureNote, staged.Subject, staged.To, staged.Cc);

    private Task RecordAsync(
        OutboundEmailFiling filing, AuditEventType eventType, string detail,
        StagedEmail staged, string? webLink, CancellationToken cancellationToken) =>
        audit.WriteAsync(
            eventType,
            detail,
            pathway: filing.PathwayLabel,
            projectId: filing.ProjectId,
            recordType: filing.RecordType,
            recordId: filing.RecordId,
            recordReference: filing.RecordReference,
            emailMessageId: staged.Id,
            webLink: webLink,
            cancellationToken: cancellationToken);
}
