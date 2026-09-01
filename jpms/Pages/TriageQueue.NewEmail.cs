using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Audit;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Todos;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;
using Jewel.JPMS.Features.Triage.Workspace;

namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    // ---- New email (fresh outbound thread from the projects mailbox) ----

    // Clears the compose form and hands its window back to whatever it showed before — pressed
    // as Cancel, and called after a successful send so the outcome banner is what remains.
    private void CloseNewEmail()
    {
        if (newEmailBusy) return;
        workspace.Close(PanelKind.Compose);
        newEmailError = null;
        newEmailTo = newEmailCc = newEmailBcc = newEmailSubject = newEmailBody = "";
        newEmailAttachments = Array.Empty<ComposeDraftAttachment>();
        newEmailFile = false;
        newEmailProjectId = "";
        newEmailRecordType = RecordType.Request;
        newEmailRecordId = "";
        newEmailRecords = Array.Empty<LinkableRecord>();
    }

    private bool NewEmailIsSendable =>
        ParseRecipients(newEmailTo).Count > 0
        && !string.IsNullOrWhiteSpace(newEmailSubject)
        && HtmlHasContent(newEmailBody)
        && (!newEmailFile || (!string.IsNullOrEmpty(newEmailProjectId) && !string.IsNullOrEmpty(newEmailRecordId)));

    private void OnNewEmailToInput(ChangeEventArgs e) => newEmailTo = e.Value?.ToString() ?? "";
    private void OnNewEmailCcInput(ChangeEventArgs e) => newEmailCc = e.Value?.ToString() ?? "";
    private void OnNewEmailBccInput(ChangeEventArgs e) => newEmailBcc = e.Value?.ToString() ?? "";
    private void OnNewEmailSubjectInput(ChangeEventArgs e) => newEmailSubject = e.Value?.ToString() ?? "";

    private void OnNewEmailBodyChanged(string html) => newEmailBody = html;
    private void OnNewEmailAttachmentsChanged(IReadOnlyList<ComposeDraftAttachment> attachments) =>
        newEmailAttachments = attachments;

    private void OnNewEmailFileToggled(ChangeEventArgs e)
    {
        newEmailFile = e.Value is true;
        if (!newEmailFile) { newEmailRecordId = ""; }
    }

    private async Task OnNewEmailProjectChanged(ChangeEventArgs e)
    {
        newEmailProjectId = e.Value?.ToString() ?? "";
        newEmailRecordId = "";
        await LoadNewEmailRecordsAsync();
    }

    private async Task OnNewEmailRecordTypeChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var t)) newEmailRecordType = (RecordType)t;
        newEmailRecordId = "";
        await LoadNewEmailRecordsAsync();
    }

    private void OnNewEmailRecordChanged(ChangeEventArgs e) => newEmailRecordId = e.Value?.ToString() ?? "";

    private async Task LoadNewEmailRecordsAsync()
    {
        newEmailRecords = Array.Empty<LinkableRecord>();
        if (string.IsNullOrEmpty(newEmailProjectId)) return;
        newEmailRecordsLoading = true;
        try { newEmailRecords = await Intake.ListLinkableRecordsAsync(newEmailProjectId, newEmailRecordType); }
        catch { newEmailError = "Couldn't load the records for that project. Please try again."; }
        finally { newEmailRecordsLoading = false; }
    }

    private async Task DoSendNewEmail(bool saveAsDraftOnly)
    {
        if (newEmailBusy) return;
        var to = ParseRecipients(newEmailTo);
        if (to.Count == 0) { newEmailError = "Add a To recipient."; return; }
        if (string.IsNullOrWhiteSpace(newEmailSubject)) { newEmailError = "Write a subject."; return; }
        if (!HtmlHasContent(newEmailBody)) { newEmailError = "Write the email first."; return; }

        var command = new SendMailboxEmail(
            ReplyToMessageId: null,
            ReplyToInternetMessageId: null,
            To: to,
            Cc: ParseRecipients(newEmailCc),
            Bcc: ParseRecipients(newEmailBcc),
            Subject: newEmailSubject.Trim(),
            Body: newEmailBody,
            BodyIsHtml: true,
            Attachments: newEmailAttachments.Select(a => a.ToRef()).ToList(),
            SaveAsDraftOnly: saveAsDraftOnly,
            Pathway: null,
            MarkThreadHandled: false,
            LinkRecordType: newEmailFile && !string.IsNullOrEmpty(newEmailRecordId) ? newEmailRecordType : null,
            LinkRecordId: newEmailFile && !string.IsNullOrEmpty(newEmailRecordId) ? newEmailRecordId : null,
            ProjectId: newEmailFile && !string.IsNullOrEmpty(newEmailProjectId) ? newEmailProjectId : null);
        var uploadParts = UploadPartsOf(newEmailAttachments);

        newEmailError = null;
        newEmailBusy = true;
        try
        {
            composeOutcome = await Intake.SendComposedEmailAsync(command, uploadParts);
            newEmailBusy = false;
            CloseNewEmail();
        }
        catch (CommandFailedException ex)
        {
            newEmailError = ex.Message;
        }
        catch
        {
            newEmailError = "The send didn't complete. Please try again.";
        }
        finally { newEmailBusy = false; }
    }

    private void OnReplyBodyInput(ChangeEventArgs e) => replyBody = e.Value?.ToString() ?? "";

    private async Task DoRestore()
    {
        if (selected is null || busy) return;
        actionError = null;
        try
        {
            busyLabel = "Restoring";
            busy = true;
            await Intake.RestoreMessageAsync(selected.Id, selected.InternetMessageId);
            selected = null;
            detail = null;
            detailLoading = false;
            ReturnWorkspaceToQueue();
            await ReloadDiscardedInPlaceAsync();
        }
        catch
        {
            actionError = "Couldn't restore that email. Please try again.";
        }
        finally { busy = false; }
    }

}
