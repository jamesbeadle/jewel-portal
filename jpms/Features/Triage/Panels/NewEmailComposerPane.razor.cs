using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Features.Triage;

namespace Jewel.JPMS.Features.Triage.Panels;

public partial class NewEmailComposerPane
{
    /// <summary>The projects a picker may offer, given what is already chosen — the host's one
    /// completed-projects rule (the ProjectStageFilter toggle, with an already-chosen completed
    /// project kept visible) applied to both the attachment sources and the filing picker.</summary>
    [Parameter, EditorRequired] public Func<string?, IReadOnlyList<Project>> ProjectOptionsFor { get; set; } = default!;

    /// <summary>Every record type an email can be filed to, and how each reads — the host page's
    /// linking vocabulary, shared with its Tagged picker so the two can never disagree.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<RecordType> LinkableRecordTypes { get; set; } = default!;
    [Parameter, EditorRequired] public Func<RecordType, string> RecordTypeLabel { get; set; } = default!;

    /// <summary>The pane is finished with its window — Cancel, or a send that just completed.
    /// The host closes the workspace pane; the form is already reset.</summary>
    [Parameter] public EventCallback OnClosed { get; set; }

    /// <summary>A send (or save-as-draft) completed — the outcome feeds the host's banner.</summary>
    [Parameter] public EventCallback<ComposeOutcome> OnSent { get; set; }

    private bool busy;
    private string? error;
    private string toField = "";
    private string ccField = "";
    private string bccField = "";
    private string subject = "";
    private string body = "";
    private IReadOnlyList<ComposeDraftAttachment> attachments = Array.Empty<ComposeDraftAttachment>();
    private bool fileToRecord;
    private string projectId = "";
    private RecordType recordType = RecordType.Request;
    private string recordId = "";
    private bool recordsLoading;
    private IReadOnlyList<LinkableRecord> records = Array.Empty<LinkableRecord>();

    private bool IsSendable =>
        MailCompose.ParseRecipients(toField).Count > 0
        && !string.IsNullOrWhiteSpace(subject)
        && MailCompose.HtmlHasContent(body)
        && (!fileToRecord || (!string.IsNullOrEmpty(projectId) && !string.IsNullOrEmpty(recordId)));

    // Clears the form and hands its window back to whatever it showed before — pressed as
    // Cancel, and called after a successful send so the outcome banner is what remains.
    private async Task Close()
    {
        if (busy) return;
        error = null;
        toField = ccField = bccField = subject = body = "";
        attachments = Array.Empty<ComposeDraftAttachment>();
        fileToRecord = false;
        projectId = "";
        recordType = RecordType.Request;
        recordId = "";
        records = Array.Empty<LinkableRecord>();
        await OnClosed.InvokeAsync();
    }

    private void OnFileToggled(ChangeEventArgs e)
    {
        fileToRecord = e.Value is true;
        if (!fileToRecord) { recordId = ""; }
    }

    private async Task OnProjectChanged(ChangeEventArgs e)
    {
        projectId = e.Value?.ToString() ?? "";
        recordId = "";
        await LoadRecordsAsync();
    }

    private async Task OnRecordTypeChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var t)) recordType = (RecordType)t;
        recordId = "";
        await LoadRecordsAsync();
    }

    private async Task LoadRecordsAsync()
    {
        records = Array.Empty<LinkableRecord>();
        if (string.IsNullOrEmpty(projectId)) return;
        recordsLoading = true;
        try { records = await Intake.ListLinkableRecordsAsync(projectId, recordType); }
        catch { error = "Couldn't load the records for that project. Please try again."; }
        finally { recordsLoading = false; }
    }

    private async Task SendAsync(bool saveAsDraftOnly)
    {
        if (busy) return;
        var to = MailCompose.ParseRecipients(toField);
        if (to.Count == 0) { error = "Add a To recipient."; return; }
        if (string.IsNullOrWhiteSpace(subject)) { error = "Write a subject."; return; }
        if (!MailCompose.HtmlHasContent(body)) { error = "Write the email first."; return; }

        var command = new SendMailboxEmail(
            ReplyToMessageId: null,
            ReplyToInternetMessageId: null,
            To: to,
            Cc: MailCompose.ParseRecipients(ccField),
            Bcc: MailCompose.ParseRecipients(bccField),
            Subject: subject.Trim(),
            Body: body,
            BodyIsHtml: true,
            Attachments: attachments.Select(a => a.ToRef()).ToList(),
            SaveAsDraftOnly: saveAsDraftOnly,
            Pathway: null,
            MarkThreadHandled: false,
            LinkRecordType: fileToRecord && !string.IsNullOrEmpty(recordId) ? recordType : null,
            LinkRecordId: fileToRecord && !string.IsNullOrEmpty(recordId) ? recordId : null,
            ProjectId: fileToRecord && !string.IsNullOrEmpty(projectId) ? projectId : null);
        var uploadParts = MailCompose.UploadPartsOf(attachments);

        error = null;
        busy = true;
        try
        {
            var outcome = await Intake.SendComposedEmailAsync(command, uploadParts);
            busy = false;
            await OnSent.InvokeAsync(outcome);
            await Close();
        }
        catch (CommandFailedException ex)
        {
            error = ex.Message;
        }
        catch
        {
            error = "The send didn't complete. Please try again.";
        }
        finally { busy = false; }
    }
}
