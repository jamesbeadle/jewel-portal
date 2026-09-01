using static Jewel.JPMS.MoneyFormats;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Features.BuildingControl;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Pages;

public partial class ProjectBuildingControlInspection
{
    [Parameter] public string ProjectId { get; set; } = "";
    [Parameter] public string InspectionId { get; set; } = "";

    private bool dataFailed;
    private bool busy;
    private string? actionError;

    private string editStageName = "";
    private string editBookedFor = "";
    private string editInspectedAt = "";
    private string editOutcomeNotes = "";
    private string editInspectorName = "";
    private string editsSeededFor = "";

    private IReadOnlyList<MailboxMessage>? emails;
    private IReadOnlyList<MailboxMessage> Emails => emails ?? Array.Empty<MailboxMessage>();
    private bool emailsFailed;
    private MailboxMessage? replyingTo;
    private bool isForward;
    private string? sentNote;

    private BuildingControlProjectView? View => BuildingControl.Current(ProjectId);

    private BuildingControlInspection? Inspection =>
        View?.Inspections.FirstOrDefault(i => i.BuildingControlInspectionId == InspectionId);

    private IReadOnlyList<BuildingControlAttachment> Files =>
        (View?.Attachments ?? Array.Empty<BuildingControlAttachment>())
            .Where(a => a.BuildingControlInspectionId == InspectionId).ToList();

    private IReadOnlyList<BuildingControlAttachment> Photos => Files.Where(f => f.IsImage).ToList();
    private IReadOnlyList<BuildingControlAttachment> Documents => Files.Where(f => !f.IsImage).ToList();

    private string OwnTag => Inspection is { } inspection ? $"JPMS/{inspection.Reference}" : "";

    protected override async Task OnInitializedAsync()
    {
        BuildingControl.OnChanged += OnStoreChanged;
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        try { await BuildingControl.RefreshAsync(ProjectId, CancellationToken.None); }
        catch { dataFailed = true; } // the query client has already toasted the detail
        SeedEdits();
        await ReloadEmailsAsync();
    }

    // The edit fields seed once per inspection from the loaded record — refreshes never clobber
    // half-typed edits (KeyedPageRouteView recreates the page per route values, so a different
    // inspection is a fresh page).
    private void SeedEdits()
    {
        if (Inspection is not { } inspection || editsSeededFor == inspection.BuildingControlInspectionId) return;
        editsSeededFor = inspection.BuildingControlInspectionId;
        editStageName = inspection.StageName;
        editBookedFor = DateInput(inspection.BookedFor);
        editInspectedAt = DateInput(inspection.InspectedAt);
        editOutcomeNotes = inspection.OutcomeNotes;
        editInspectorName = inspection.InspectorName;
    }

    private void OnStoreChanged()
    {
        SeedEdits();
        StateHasChanged();
    }

    private bool HasEdits(BuildingControlInspection inspection) =>
        editStageName.Trim() != inspection.StageName
        || ParseDate(editBookedFor) != inspection.BookedFor
        || ParseDate(editInspectedAt) != inspection.InspectedAt
        || editOutcomeNotes.Trim() != inspection.OutcomeNotes
        || editInspectorName.Trim() != inspection.InspectorName;

    private async Task SaveAsync(BuildingControlInspection inspection)
    {
        if (string.IsNullOrWhiteSpace(editStageName)) return;
        busy = true;
        actionError = null;
        try
        {
            await Commands.SendAsync(new UpdateBuildingControlInspection(
                inspection.BuildingControlInspectionId,
                new BuildingControlInspectionDetails(
                    editStageName.Trim(), ParseDate(editBookedFor), ParseDate(editInspectedAt),
                    editOutcomeNotes.Trim(), editInspectorName.Trim())), CancellationToken.None);
            editsSeededFor = ""; // re-seed from the saved answer (status may have moved with the date)
            await RefreshAfterWriteAsync();
        }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; }
    }

    private async Task SetStatusAsync(BuildingControlInspection inspection, ChangeEventArgs e)
    {
        if (!int.TryParse(e.Value?.ToString(), out var statusValue)) return;
        var status = (BuildingControlInspectionStatus)statusValue;
        if (status == inspection.Status) return;
        busy = true;
        actionError = null;
        try
        {
            await Commands.SendAsync(new SetBuildingControlInspectionStatus(
                inspection.BuildingControlInspectionId, status), CancellationToken.None);
            editsSeededFor = ""; // InspectedAt may have been stamped/cleared by the move
            await RefreshAfterWriteAsync();
        }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; }
    }

    private async Task UploadAsync(BuildingControlInspection inspection, InputFileChangeEventArgs e, BuildingControlAttachmentKind? kind)
    {
        busy = true;
        actionError = null;
        try
        {
            await Attachments.UploadToInspectionAsync(inspection.BuildingControlInspectionId, kind, e.GetMultipleFiles(20));
            await RefreshAfterWriteAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { actionError = ex.Message; }
        finally { busy = false; }
    }

    private async Task RemoveFileAsync(string attachmentId)
    {
        busy = true;
        actionError = null;
        try
        {
            await Commands.SendAsync(new RemoveBuildingControlAttachment(attachmentId), CancellationToken.None);
            await RefreshAfterWriteAsync();
        }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; }
    }

    private async Task CopyEmailFilesAsync(BuildingControlInspection inspection, MailboxMessage email)
    {
        busy = true;
        actionError = null;
        try
        {
            // Every file attachment on the email; kinds are inferred server-side (images →
            // Photo, PDFs → Site inspection report). The detail read gives the Graph ids the
            // copy route needs.
            var detail = await Intake.GetMessageDetailAsync(email.Id, email.InternetMessageId, CancellationToken.None);
            var attachmentIds = detail.Attachments
                .Where(a => !string.IsNullOrWhiteSpace(a.Id))
                .Select(a => a.Id)
                .ToList();
            if (attachmentIds.Count == 0)
            {
                actionError = "That email's attachments couldn't be listed — open it in the Control Centre instead.";
                return;
            }
            await Commands.SendAsync(new CopyEmailAttachmentsToBuildingControlInspection(
                inspection.BuildingControlInspectionId, email.Id, attachmentIds), CancellationToken.None);
            await RefreshAfterWriteAsync();
        }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; }
    }

    private async Task ReloadEmailsAsync()
    {
        if (Inspection is null) { emails = Array.Empty<MailboxMessage>(); return; }
        try
        {
            emails = await Queries.AskAsync(
                new ListRecordEmails(RecordType.BuildingControlInspection, InspectionId), CancellationToken.None);
            emailsFailed = false;
        }
        catch { emailsFailed = true; }
    }

    private void StartCompose(MailboxMessage email, bool forward)
    {
        sentNote = null;
        isForward = forward;
        replyingTo = email;
    }

    private async Task OnSent(ComposeOutcome outcome)
    {
        replyingTo = null;
        sentNote = outcome.Sent ? $"Sent: {outcome.Subject}" : $"Saved as a draft in Outlook: {outcome.Subject}";
        await ReloadEmailsAsync();
    }

    // Post-write reload: swallow query failures (the toast already reported them) so a 502 on
    // the refetch can't take the page down after a successful write — see post-write-reload rule.
    private async Task RefreshAfterWriteAsync()
    {
        try { await BuildingControl.RefreshAsync(ProjectId, CancellationToken.None); }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private static string SourceLabel(BuildingControlAttachmentSource source) =>
        source == BuildingControlAttachmentSource.Email ? "from the linked email," : "uploaded";

    private static string DateInput(DateTimeOffset? value) =>
        value is { } date ? date.ToString("yyyy-MM-dd") : "";

    private static DateTimeOffset? ParseDate(string text) =>
        DateTime.TryParseExact(text, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var value)
            ? new DateTimeOffset(value, TimeSpan.Zero)
            : null;


    public void Dispose() => BuildingControl.OnChanged -= OnStoreChanged;
}
