using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Features.Site;

public partial class ProgrammeWorkbench : IDisposable
{
    [Inject] private IQueryClient Queries { get; set; } = default!;
    [Inject] private ICommandSender Commands { get; set; } = default!;
    [Inject] private IRequestRegister RequestRegister { get; set; } = default!;
    [Inject] private AuthService Auth { get; set; } = default!;

    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";
    /// <summary>The human wants a Notice of Delay raised from this delay event — the page's Claims view takes it.</summary>
    [Parameter] public EventCallback<ProgrammeDelayEvent> OnRaiseNod { get; set; }

    private enum Form { None, AddTask, AddLink, Baseline }
    private Form openForm = Form.None;

    private bool loaded;
    private bool busy;
    private string? error;
    private ProgrammeDetail? programme;

    private string newTaskTitle = "";
    private DateTime? newTaskStart;
    private DateTime? newTaskEnd;

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
    // The register's NODs decide whether the slippage banner nags — it re-renders when they change.
    private bool HasNodSinceBaseline =>
        programme?.Baseline is { } baseline
        && RequestRegister.ForProject(ProjectId, RequestType.NoticeOfDelay).Any(n => n.RaisedAt >= baseline.TakenAt);

    protected override async Task OnInitializedAsync()
    {
        RequestRegister.OnChange += StateHasChanged;
        await LoadAsync();
    }

    public void Dispose() => RequestRegister.OnChange -= StateHasChanged;

    private async Task LoadAsync()
    {
        error = null;
        try
        {
            programme = await Queries.AskAsync(new GetProgrammeDetail(ProjectId), CancellationToken.None);
        }
        catch
        {
            programme = null;
            error = "Couldn't load the programme. Please try again.";
        }
        finally
        {
            loaded = true;
        }
    }

    private void ToggleForm(Form form)
    {
        openForm = openForm == form ? Form.None : form;
        error = null;
        confirmingRemoveBaselineId = null;
    }

    /// <summary>One shape for every write: busy on, error off, the command, re-read; the refusal
    /// wording is the caller's. Answers whether it took, so an editor can decide to close.</summary>
    private async Task<bool> WriteAsync<TResult>(ICommand<TResult> command, string refusal)
    {
        if (busy) return false;
        busy = true;
        error = null;
        try
        {
            await Commands.SendAsync(command, CancellationToken.None);
            await LoadAsync();
            return true;
        }
        catch
        {
            error = refusal;
            return false;
        }
        finally
        {
            busy = false;
        }
    }

    private async Task AddTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(newTaskTitle) || newTaskStart is null || newTaskEnd is null) return;
        var added = await WriteAsync(new AddProgrammeTask(
            ProjectId, newTaskTitle.Trim(),
            // Date-only values: pin to UTC midnight so the stored instant is timezone-stable.
            new DateTimeOffset(newTaskStart.Value.Date, TimeSpan.Zero),
            new DateTimeOffset(newTaskEnd.Value.Date, TimeSpan.Zero),
            BoqLineItemId: null), "Couldn't add the task. Please try again.");
        if (!added) return;
        newTaskTitle = "";
        newTaskStart = null;
        newTaskEnd = null;
        openForm = Form.None;
    }

    private Task<bool> SaveTaskAsync(ProgrammeGanttChart.TaskEdit edit) =>
        WriteAsync(new UpdateProgrammeTask(
            edit.ProgrammeTaskId, edit.Title,
            new DateTimeOffset(edit.Start, TimeSpan.Zero),
            new DateTimeOffset(edit.End, TimeSpan.Zero),
            edit.Progress, edit.BoqLineItemId), "Couldn't save the task. Please try again.");

    private Task<bool> RemoveTaskAsync(string programmeTaskId) =>
        WriteAsync(new RemoveProgrammeTask(programmeTaskId), "Couldn't remove the task. Please try again.");

    private async Task AddLinkAsync()
    {
        if (string.IsNullOrWhiteSpace(linkPredecessorId) || string.IsNullOrWhiteSpace(linkSuccessorId)) return;
        var added = await WriteAsync(new AddProgrammeTaskLink(ProjectId, linkPredecessorId, linkSuccessorId, linkLagDays),
            "Couldn't add the dependency — check it doesn't duplicate or create a cycle.");
        if (!added) return;
        linkPredecessorId = "";
        linkSuccessorId = "";
        linkLagDays = 0;
        openForm = Form.None;
    }

    private Task RemoveLinkAsync(string programmeTaskLinkId) =>
        WriteAsync(new RemoveProgrammeTaskLink(programmeTaskLinkId), "Couldn't remove the dependency. Please try again.");

    private async Task TakeBaselineAsync()
    {
        if (string.IsNullOrWhiteSpace(baselineLabel)) return;
        var taken = await WriteAsync(new TakeProgrammeBaseline(ProjectId, baselineLabel.Trim(), Auth.CurrentUser!.Email),
            "Couldn't take the baseline. Please try again.");
        if (!taken) return;
        baselineLabel = "";
        openForm = Form.None;
    }

    private async Task RemoveBaselineAsync(string programmeBaselineId)
    {
        if (await WriteAsync(new RemoveProgrammeBaseline(programmeBaselineId), "Couldn't remove the baseline. Please try again."))
            confirmingRemoveBaselineId = null;
    }
}
