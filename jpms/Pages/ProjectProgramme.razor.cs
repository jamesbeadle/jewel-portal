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
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Pages;

public partial class ProjectProgramme
{
    [Parameter] public string ProjectId { get; set; } = "";

    private enum SubView { Programme, Claims, CriticalRfis, RelevantEvents }
    private SubView view = SubView.Programme;

    private enum ClaimForm { None, Nod, Eot, Lad }
    private ClaimForm openForm = ClaimForm.None;

    // Session checked and the user is signed in — not "the data is here". The heading and the
    // sub-tabs show straight away; each view holds its own panels until their sources land.
    private bool emailsLoaded;
    private string? emailsError;
    private IReadOnlyList<MailboxMessage> emails = Array.Empty<MailboxMessage>();

    // Claims state. NOD/EOT come from the request register (they ARE requests, of those kinds);
    // LADs claims come from their own store.
    private bool ladsLoaded;
    // Kept apart from claimsError, which also carries the raise/record failures: only a failed
    // *fetch* means the empty list is not an answer.
    private bool ladsFailed;
    private string? claimsError;
    private bool claimsBusy;
    private IReadOnlyList<LadClaim> lads = Array.Empty<LadClaim>();

    // Raise-NOD form.
    private string nodTitle = "";
    private string nodDescription = "";

    // Raise-EOT form.
    private string eotTitle = "";
    private string eotDescription = "";
    private string eotRelatedNodId = "";

    // Record-LADs form.
    private string ladTitle = "";
    private string ladDescription = "";
    private DateTime? ladPeriodFrom;
    private DateTime? ladPeriodTo;
    private int ladDaysClaimed;
    private decimal ladRatePerWeek;
    private decimal ladAmount;

    // The register backs the NODs, the EOTs and the critical-path RFIs alike: until it has landed
    // an empty list is indistinguishable from a project with no claims at all.
    private bool RequestsReady => RequestRegister.LoadedFor(ProjectId);

    private IReadOnlyList<Request> Nods => RequestRegister.ForProject(ProjectId, RequestType.NoticeOfDelay);
    private IReadOnlyList<Request> Eots => RequestRegister.ForProject(ProjectId, RequestType.ExtensionOfTime);
    private int ClaimsCount => Nods.Count + Eots.Count + (ladsLoaded ? lads.Count : 0);

    // RFIs tagged Critical Path (set on the RFI's detail page), open first then by reference —
    // outstanding programme blockers lead, answered ones stay below as the impact history. The
    // register is the same stale-while-revalidate store the claims read from, so the page-entry
    // Refresh in OnInitializedAsync keeps this subset current too.
    private IReadOnlyList<Request> CriticalRfis => RequestRegister
        .ForProject(ProjectId, RequestType.Rfi)
        .Where(r => r.CriticalPath)
        .OrderBy(r => r.Status is RequestStatus.Closed ? 1 : 0)
        .ThenBy(r => r.Reference, StringComparer.OrdinalIgnoreCase)
        .ToList();

    // The tab badge counts only the open ones — the number that still gates the programme.
    private int OpenCriticalRfiCount => CriticalRfis.Count(r => r.Status is not RequestStatus.Closed);

    // Programme state. The detail (tasks, links, latest baseline) comes down in one query; movement
    // is computed locally with the same calculator the API's Programme Agent uses, so the banner
    // here and the agent's delay analysis always agree.
    private enum ProgrammeForm { None, AddTask, AddLink, Baseline }
    private ProgrammeForm openProgrammeForm = ProgrammeForm.None;

    private bool programmeLoaded;
    private bool programmeBusy;
    private string? programmeError;
    private ProgrammeDetail? programme;

    private string newTaskTitle = "";
    private DateTime? newTaskStart;
    private DateTime? newTaskEnd;

    private string? editingTaskId;
    private string editTitle = "";
    private DateTime? editStart;
    private DateTime? editEnd;
    private decimal editProgress;
    private string? editBoqLineItemId;
    private bool confirmingRemoveTask;

    private string linkPredecessorId = "";
    private string linkSuccessorId = "";
    private int linkLagDays;

    private string baselineLabel = "";
    private string? confirmingRemoveBaselineId;

    private IReadOnlyList<ProgrammeTask> Tasks => programme?.Tasks ?? Array.Empty<ProgrammeTask>();
    private IReadOnlyList<ProgrammeBaseline> Baselines => programme?.Baselines ?? Array.Empty<ProgrammeBaseline>();
    private IReadOnlyList<ProgrammeTaskLink> Links => programme?.Links ?? Array.Empty<ProgrammeTaskLink>();
    private IReadOnlyList<ProgrammeBaselineTask> BaselineTasks => programme?.BaselineTasks ?? Array.Empty<ProgrammeBaselineTask>();
    private ProgrammeMovement Movement => ProgrammeMovementCalculator.Compare(Tasks, BaselineTasks);
    private bool HasNodSinceBaseline =>
        programme?.Baseline is { } baseline && Nods.Any(n => n.RaisedAt >= baseline.TakenAt);

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        RequestRegister.OnChange += StateHasChanged;
        // Refresh on entry so NoD / EOT rows and count badges reflect changes made since the
        // register was first cached (stale-while-revalidate).
        RequestRegister.Refresh(ProjectId);
        // Load the programme, claims + relevant events in the background so the default view and the
        // count badges are ready when the tabs are opened.
        await Task.WhenAll(LoadProgrammeAsync(), LoadLadsAsync(), LoadEmailsAsync());
    }

    public void Dispose() => RequestRegister.OnChange -= StateHasChanged;

    private void SwitchView(SubView next) => view = next;

    private void ToggleForm(ClaimForm form)
    {
        openForm = openForm == form ? ClaimForm.None : form;
        claimsError = null;
    }

    private async Task LoadProgrammeAsync()
    {
        programmeError = null;
        try
        {
            programme = await Queries.AskAsync(new GetProgrammeDetail(ProjectId), CancellationToken.None);
        }
        catch
        {
            programme = null;
            programmeError = "Couldn't load the programme. Please try again.";
        }
        finally
        {
            programmeLoaded = true;
        }
    }

    private void ToggleProgrammeForm(ProgrammeForm form)
    {
        openProgrammeForm = openProgrammeForm == form ? ProgrammeForm.None : form;
        programmeError = null;
        confirmingRemoveBaselineId = null;
    }

    private async Task AddTaskAsync()
    {
        if (programmeBusy || string.IsNullOrWhiteSpace(newTaskTitle) || newTaskStart is null || newTaskEnd is null) return;
        programmeBusy = true;
        programmeError = null;
        try
        {
            await Commands.SendAsync(new AddProgrammeTask(
                ProjectId, newTaskTitle.Trim(),
                // Date-only values: pin to UTC midnight so the stored instant is timezone-stable.
                new DateTimeOffset(newTaskStart.Value.Date, TimeSpan.Zero),
                new DateTimeOffset(newTaskEnd.Value.Date, TimeSpan.Zero),
                BoqLineItemId: null), CancellationToken.None);
            newTaskTitle = "";
            newTaskStart = null;
            newTaskEnd = null;
            openProgrammeForm = ProgrammeForm.None;
            await LoadProgrammeAsync();
        }
        catch
        {
            programmeError = "Couldn't add the task. Please try again.";
        }
        finally
        {
            programmeBusy = false;
        }
    }

    private void BeginEditTask(ProgrammeTask task)
    {
        editingTaskId = editingTaskId == task.ProgrammeTaskId ? null : task.ProgrammeTaskId;
        editTitle = task.Title;
        editStart = task.PlannedStart.UtcDateTime.Date;
        editEnd = task.PlannedEnd.UtcDateTime.Date;
        editProgress = task.ProgressPercent;
        editBoqLineItemId = task.BoqLineItemId;
        confirmingRemoveTask = false;
    }

    private void CancelEditTask()
    {
        editingTaskId = null;
        confirmingRemoveTask = false;
    }

    private async Task SaveEditTaskAsync()
    {
        if (programmeBusy || editingTaskId is null || string.IsNullOrWhiteSpace(editTitle) || editStart is null || editEnd is null) return;
        programmeBusy = true;
        programmeError = null;
        try
        {
            await Commands.SendAsync(new UpdateProgrammeTask(
                editingTaskId, editTitle.Trim(),
                new DateTimeOffset(editStart.Value.Date, TimeSpan.Zero),
                new DateTimeOffset(editEnd.Value.Date, TimeSpan.Zero),
                Math.Clamp(editProgress, 0, 100),
                editBoqLineItemId), CancellationToken.None);
            editingTaskId = null;
            await LoadProgrammeAsync();
        }
        catch
        {
            programmeError = "Couldn't save the task. Please try again.";
        }
        finally
        {
            programmeBusy = false;
        }
    }

    private async Task RemoveTaskAsync()
    {
        if (programmeBusy || editingTaskId is null) return;
        programmeBusy = true;
        programmeError = null;
        try
        {
            await Commands.SendAsync(new RemoveProgrammeTask(editingTaskId), CancellationToken.None);
            editingTaskId = null;
            confirmingRemoveTask = false;
            await LoadProgrammeAsync();
        }
        catch
        {
            programmeError = "Couldn't remove the task. Please try again.";
        }
        finally
        {
            programmeBusy = false;
        }
    }

    private async Task AddLinkAsync()
    {
        if (programmeBusy || string.IsNullOrWhiteSpace(linkPredecessorId) || string.IsNullOrWhiteSpace(linkSuccessorId)) return;
        programmeBusy = true;
        programmeError = null;
        try
        {
            await Commands.SendAsync(new AddProgrammeTaskLink(
                ProjectId, linkPredecessorId, linkSuccessorId, linkLagDays), CancellationToken.None);
            linkPredecessorId = "";
            linkSuccessorId = "";
            linkLagDays = 0;
            openProgrammeForm = ProgrammeForm.None;
            await LoadProgrammeAsync();
        }
        catch
        {
            programmeError = "Couldn't add the dependency — check it doesn't duplicate or create a cycle.";
        }
        finally
        {
            programmeBusy = false;
        }
    }

    private async Task RemoveLinkAsync(string programmeTaskLinkId)
    {
        if (programmeBusy) return;
        programmeBusy = true;
        programmeError = null;
        try
        {
            await Commands.SendAsync(new RemoveProgrammeTaskLink(programmeTaskLinkId), CancellationToken.None);
            await LoadProgrammeAsync();
        }
        catch
        {
            programmeError = "Couldn't remove the dependency. Please try again.";
        }
        finally
        {
            programmeBusy = false;
        }
    }

    private async Task TakeBaselineAsync()
    {
        if (programmeBusy || string.IsNullOrWhiteSpace(baselineLabel)) return;
        programmeBusy = true;
        programmeError = null;
        try
        {
            await Commands.SendAsync(new TakeProgrammeBaseline(
                ProjectId, baselineLabel.Trim(), Auth.CurrentUser!.Email), CancellationToken.None);
            baselineLabel = "";
            openProgrammeForm = ProgrammeForm.None;
            await LoadProgrammeAsync();
        }
        catch
        {
            programmeError = "Couldn't take the baseline. Please try again.";
        }
        finally
        {
            programmeBusy = false;
        }
    }

    private async Task RemoveBaselineAsync(string programmeBaselineId)
    {
        if (programmeBusy) return;
        programmeBusy = true;
        programmeError = null;
        try
        {
            await Commands.SendAsync(new RemoveProgrammeBaseline(programmeBaselineId), CancellationToken.None);
            confirmingRemoveBaselineId = null;
            await LoadProgrammeAsync();
        }
        catch
        {
            programmeError = "Couldn't remove the baseline. Please try again.";
        }
        finally
        {
            programmeBusy = false;
        }
    }

    // Pre-fills the Notice of Delay form from a detected delay event and jumps to the Claims view —
    // the human still reviews and raises it (the notice itself is never issued automatically).
    private void RaiseNodFromDelay(ProgrammeDelayEvent delayEvent)
    {
        nodTitle = $"Delay to {delayEvent.Title} — programme impact {delayEvent.SlipDays} day(s)";
        nodDescription =
            $"The programme task \"{delayEvent.Title}\" has moved against the baselined programme: " +
            $"planned completion {delayEvent.BaselineEnd.LocalDateTime:d MMM yyyy} now forecast {delayEvent.PlannedEnd.LocalDateTime:d MMM yyyy} " +
            $"({delayEvent.SlipDays} day(s) slippage{(delayEvent.DrivesCompletion ? ", driving overall project completion" : "")}). " +
            "Cause of delay and works affected: [complete before issuing].";
        view = SubView.Claims;
        openForm = ClaimForm.Nod;
    }

}
