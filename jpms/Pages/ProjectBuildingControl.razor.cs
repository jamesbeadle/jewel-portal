using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Features.BuildingControl;

namespace Jewel.JPMS.Pages;

public partial class ProjectBuildingControl
{
    [Parameter] public string ProjectId { get; set; } = "";

    private bool sessionReady;
    private bool dataFailed;
    private bool busy;
    private string? actionError;

    // Case form state (create and edit share it).
    private bool caseFormOpen;
    private BuildingControlCase? editingCase;
    private BuildingControlRegime formRegime = BuildingControlRegime.LocalAuthority;
    private string formBodyName = "";
    private string formBodyReference = "";
    private string formContactName = "";
    private string formContactEmail = "";
    private string formContactPhone = "";
    private string formNoticeSubmittedOn = "";
    private string formAcceptedOn = "";
    private string formNotes = "";
    private bool formSeedStages = true;

    private bool addStageOpen;
    private string newStageName = "";
    private string newStageBookedFor = "";

    private BuildingControlAttachmentKind caseUploadKind = BuildingControlAttachmentKind.Notice;

    // The kinds that make sense on the CASE (stage photos and site reports live on inspections).
    private static readonly BuildingControlAttachmentKind[] CaseDocumentKinds =
    {
        BuildingControlAttachmentKind.Notice,
        BuildingControlAttachmentKind.Acknowledgement,
        BuildingControlAttachmentKind.DecisionNotice,
        BuildingControlAttachmentKind.PlanningPermission,
        BuildingControlAttachmentKind.CompletionCertificate,
        BuildingControlAttachmentKind.Other
    };

    private BuildingControlProjectView? View => BuildingControl.Current(ProjectId);

    // The working case: the newest non-lapsed/non-certified one, else the newest of anything —
    // the tab always shows what there is, even a finished case.
    private BuildingControlCase? ActiveCase
    {
        get
        {
            var cases = View?.Cases ?? Array.Empty<BuildingControlCase>();
            return cases.FirstOrDefault(c =>
                       c.Status is not (BuildingControlCaseStatus.Lapsed or BuildingControlCaseStatus.CompletionCertified))
                   ?? cases.FirstOrDefault();
        }
    }

    private IReadOnlyList<BuildingControlInspection> Inspections =>
        ActiveCase is { } activeCase
            ? (View?.Inspections ?? Array.Empty<BuildingControlInspection>())
                .Where(i => i.BuildingControlCaseId == activeCase.BuildingControlCaseId).ToList()
            : Array.Empty<BuildingControlInspection>();

    private IReadOnlyList<BuildingControlAttachment> CaseFiles(BuildingControlCase forCase) =>
        (View?.Attachments ?? Array.Empty<BuildingControlAttachment>())
            .Where(a => a.BuildingControlCaseId == forCase.BuildingControlCaseId).ToList();

    private IReadOnlyList<BuildingControlAttachment> FilesFor(BuildingControlInspection inspection) =>
        (View?.Attachments ?? Array.Empty<BuildingControlAttachment>())
            .Where(a => a.BuildingControlInspectionId == inspection.BuildingControlInspectionId).ToList();

    protected override async Task OnInitializedAsync()
    {
        BuildingControl.OnChanged += StateHasChanged;
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        sessionReady = true;
        await LoadAsync();
    }

    private string loadedForProjectId = "";

    protected override async Task OnParametersSetAsync()
    {
        // Project switch via the shell's arrows: same page, new key — load the new register
        // (and give a previously failed project a fresh chance).
        if (!sessionReady || loadedForProjectId == ProjectId) return;
        dataFailed = false;
        caseFormOpen = false;
        addStageOpen = false;
        actionError = null;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        loadedForProjectId = ProjectId;
        try { await BuildingControl.RefreshAsync(ProjectId, CancellationToken.None); }
        catch { dataFailed = true; } // the query client has already toasted the detail
    }

    private void OpenInspection(BuildingControlInspection inspection) =>
        Nav.NavigateTo($"/projects/{ProjectId}/building-control/inspections/{inspection.BuildingControlInspectionId}");

    private void OpenCreateCase()
    {
        editingCase = null;
        formRegime = BuildingControlRegime.LocalAuthority;
        formBodyName = ""; formBodyReference = "";
        formContactName = ""; formContactEmail = ""; formContactPhone = "";
        formNoticeSubmittedOn = ""; formAcceptedOn = ""; formNotes = "";
        formSeedStages = true;
        caseFormOpen = true;
    }

    private void OpenEditCase(BuildingControlCase existing)
    {
        editingCase = existing;
        formRegime = existing.Regime;
        formBodyName = existing.BodyName;
        formBodyReference = existing.BodyReference;
        formContactName = existing.ContactName;
        formContactEmail = existing.ContactEmail;
        formContactPhone = existing.ContactPhone;
        formNoticeSubmittedOn = DateInput(existing.NoticeSubmittedOn);
        formAcceptedOn = DateInput(existing.AcceptedOn);
        formNotes = existing.Notes;
        caseFormOpen = true;
    }

    private BuildingControlCaseDetails FormDetails() => new(
        formRegime, formBodyName.Trim(), formBodyReference.Trim(),
        formContactName.Trim(), formContactEmail.Trim(), formContactPhone.Trim(),
        ParseDate(formNoticeSubmittedOn), ParseDate(formAcceptedOn), formNotes.Trim());

    private async Task SaveCaseAsync()
    {
        if (string.IsNullOrWhiteSpace(formBodyName)) return;
        busy = true;
        actionError = null;
        try
        {
            if (editingCase is { } existing)
                await Commands.SendAsync(new UpdateBuildingControlCase(existing.BuildingControlCaseId, FormDetails()), CancellationToken.None);
            else
                await Commands.SendAsync(new CreateBuildingControlCase(ProjectId, FormDetails(), formSeedStages), CancellationToken.None);
            caseFormOpen = false;
            await RefreshAfterWriteAsync();
        }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; }
    }

    private async Task SetCaseStatusAsync(BuildingControlCase forCase, ChangeEventArgs e)
    {
        if (!int.TryParse(e.Value?.ToString(), out var statusValue)) return;
        var status = (BuildingControlCaseStatus)statusValue;
        if (status == forCase.Status) return;
        busy = true;
        actionError = null;
        try
        {
            await Commands.SendAsync(new SetBuildingControlCaseStatus(forCase.BuildingControlCaseId, status), CancellationToken.None);
            await RefreshAfterWriteAsync();
        }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; }
    }

    private async Task AddStageAsync()
    {
        if (ActiveCase is not { } activeCase || string.IsNullOrWhiteSpace(newStageName)) return;
        busy = true;
        actionError = null;
        try
        {
            await Commands.SendAsync(new AddBuildingControlInspection(
                activeCase.BuildingControlCaseId,
                new BuildingControlInspectionDetails(newStageName.Trim(), ParseDate(newStageBookedFor), null, "", "")), CancellationToken.None);
            newStageName = ""; newStageBookedFor = "";
            addStageOpen = false;
            await RefreshAfterWriteAsync();
        }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; }
    }

    private async Task DeleteStageAsync(BuildingControlInspection inspection)
    {
        busy = true;
        actionError = null;
        try
        {
            await Commands.SendAsync(new DeleteBuildingControlInspection(inspection.BuildingControlInspectionId), CancellationToken.None);
            await RefreshAfterWriteAsync();
        }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; }
    }

    private async Task UploadToCaseAsync(BuildingControlCase forCase, InputFileChangeEventArgs e)
    {
        busy = true;
        actionError = null;
        try
        {
            await Attachments.UploadToCaseAsync(forCase.BuildingControlCaseId, caseUploadKind, e.GetMultipleFiles(20));
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

    // Post-write reload: swallow query failures (the toast already reported them) so a 502 on the
    // refetch can't take the page down after a successful write — see post-write-reload rule.
    private async Task RefreshAfterWriteAsync()
    {
        try { await BuildingControl.RefreshAsync(ProjectId, CancellationToken.None); }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private string FileCountText(BuildingControlInspection inspection)
    {
        var files = FilesFor(inspection);
        if (files.Count == 0) return "—";
        var photos = files.Count(f => f.IsImage);
        var documents = files.Count - photos;
        return (photos, documents) switch
        {
            (> 0, > 0) => $"{photos} photo{Plural(photos)}, {documents} doc{Plural(documents)}",
            (> 0, _)   => $"{photos} photo{Plural(photos)}",
            _          => $"{documents} doc{Plural(documents)}"
        };
    }

    private static string Plural(int count) => count == 1 ? "" : "s";

    private static string StatusClass(BuildingControlInspectionStatus status) => status switch
    {
        BuildingControlInspectionStatus.Passed => "bg-positive/10 text-positive",
        BuildingControlInspectionStatus.ActionsRequired => "bg-negative/10 text-negative",
        BuildingControlInspectionStatus.Booked or BuildingControlInspectionStatus.Inspected => "bg-accent/10 text-accent",
        _ => "bg-surface-raised text-content-muted"
    };

    private static string DateText(DateTimeOffset? value) =>
        value is { } date ? date.ToString("d MMM yyyy") : "—";

    private static string DateInput(DateTimeOffset? value) =>
        value is { } date ? date.ToString("yyyy-MM-dd") : "";

    private static DateTimeOffset? ParseDate(string text) =>
        DateTime.TryParseExact(text, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var value)
            ? new DateTimeOffset(value, TimeSpan.Zero)
            : null;

    private static BuildingControlRegime ParseRegime(ChangeEventArgs e) =>
        int.TryParse(e.Value?.ToString(), out var value) ? (BuildingControlRegime)value : BuildingControlRegime.LocalAuthority;

    private static BuildingControlAttachmentKind? ParseKind(ChangeEventArgs e) =>
        int.TryParse(e.Value?.ToString(), out var value) ? (BuildingControlAttachmentKind)value : null;


    public void Dispose() => BuildingControl.OnChanged -= StateHasChanged;
}
