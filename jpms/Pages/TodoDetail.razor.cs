using Jewel.JPMS.Components;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Todos;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components;

namespace Jewel.JPMS.Pages;

// The to-do page's state and commands. The page owns the ITEM (and the actions that change it);
// the linked-to-dos and communications panels own their reads, each gating its own region. The
// gate mirrors are the same expressions the To-dos browser uses — the API's role sets decide for
// real, these only decide what controls to offer.
public partial class TodoDetail
{
    [Parameter] public string TodoItemId { get; set; } = "";

    // Session checked and the user signed in — NOT "the data is here"; every data-bearing region
    // gates on its own sources.
    private bool sessionReady;
    private bool itemLoading = true;
    private bool loadFailed;
    private TodoItem? item;
    private bool busy;
    private string? error;
    private bool deleteArmed;
    // Bumped after every item-changing command so the timeline panel re-reads its lines.
    private int activityVersion;
    private IReadOnlyList<SearchSelect.Option> assigneeOptions = Array.Empty<SearchSelect.Option>();

    // Mirrors the API's TodoRoles.AllowedToManageTodos gate (reassign, move, delete).
    private bool CanManage =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector
            or Role.ProjectManager or Role.SiteManager or Role.Accounts);

    // Mirrors the API's TodoRoles.AllowedToSeeAllTodos gate — who may re-file an item company-wide.
    private bool CanSeeAll =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector);

    // Mirrors the API's JpmsRoleSets.AllInternal — who may read to-dos (and send from the
    // projects mailbox, the compose gate widened to match on 2026-08-10).
    private bool HasInternalRole =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector
            or Role.ProjectManager or Role.QuantitySurveyor or Role.SiteManager or Role.HealthSafetyOfficer
            or Role.OfficeComplianceCoordinator or Role.OfficeAdmin or Role.Foreman or Role.Accounts);

    // "Mine" = assigned to a role the signed-in user holds, and not pinned to a different person —
    // the same rule UpdateTodoItemAuthorisation applies for the tick-off path.
    private bool IsMine =>
        item is not null
        && item.AssigneeRole is Role role && Session.AvailableRoles.Contains(role)
        && (item.AssigneePersonEmail is null
            || string.Equals(item.AssigneePersonEmail, Auth.CurrentUser?.Email, StringComparison.OrdinalIgnoreCase));

    private bool CanComplete => CanManage || IsMine;

    // Mirrors the API's TriageRoles.AllowedToTriage — who may file an unfiled reply to the item
    // (LinkMessageToRecord is a triage act).
    private bool CanTriage =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector
            or Role.ProjectManager or Role.FinanceDirector);

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        sessionReady = true;
        StateHasChanged();
        if (!Session.IsApproved || !HasInternalRole) { itemLoading = false; return; }

        // The item, the project labels and the assignee pool are independent — they go out
        // together rather than one after another.
        var loads = new List<Task> { LoadItemAsync(), LoadProjectLabelsAsync() };
        if (CanManage) loads.Add(LoadAssigneeOptionsAsync());
        await Task.WhenAll(loads);
    }

    private async Task LoadItemAsync()
    {
        try { item = await TodoStore.GetAsync(TodoItemId); }
        catch { loadFailed = true; }
        finally { itemLoading = false; }
    }

    // Labels only — a failed project read degrades the scope line, never the page.
    private async Task LoadProjectLabelsAsync()
    {
        try { if (Projects.Current is null) await Projects.RefreshAsync(CancellationToken.None); } catch { }
    }

    // A failed load leaves the pool empty, which hides the reassign control — the page's own job
    // is untouched.
    private async Task LoadAssigneeOptionsAsync()
    {
        try
        {
            var rolesTask = TodoStore.ListAssignableRolesAsync();
            var peopleTask = TodoStore.ListAssignablePeopleAsync();
            assigneeOptions = TodoAssigneePicker.BuildOptions(await rolesTask, await peopleTask);
        }
        catch { }
    }

    private IReadOnlyList<Project> ProjectPool =>
        Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>();

    private string ScopeLabel(TodoItem forItem)
    {
        if (string.IsNullOrWhiteSpace(forItem.ProjectId)) return "Company-wide";
        var project = ProjectPool.FirstOrDefault(candidate => candidate.ProjectId == forItem.ProjectId);
        return project is null ? "Project" : $"{project.Reference} — {project.Name}";
    }

    // The move picker's destination pool: every project the item could go to (minus wherever it is
    // now — every offered row is a real move) and, for the MD / administrators only, the
    // company-wide row. Mirrors MoveTodoItemAuthorisation.
    private IReadOnlyList<SearchSelect.Option> MoveOptions
    {
        get
        {
            if (item is null) return Array.Empty<SearchSelect.Option>();
            var options = new List<SearchSelect.Option>();
            if (CanSeeAll && !string.IsNullOrWhiteSpace(item.ProjectId))
                options.Add(new SearchSelect.Option(TodoScope.General, "Company-wide — no project"));
            options.AddRange(ProjectPool
                .Where(project => project.ProjectId != item.ProjectId)
                .Select(project => new SearchSelect.Option(project.ProjectId, $"{project.Reference} — {project.Name}")));
            return options;
        }
    }

    private async Task ToggleCompleteAsync()
    {
        if (item is null) return;
        deleteArmed = false;
        await RunAsync(() => TodoStore.UpdateAsync(new UpdateTodoItem(
            item.TodoItemId, item.Title, NullIfBlank(item.Notes),
            item.AssigneeRole, item.AssigneePersonEmail, item.DueAt,
            !item.IsComplete)));
    }

    private async Task<bool> ReassignAsync(TodoAssignee? assignee)
    {
        if (item is null) return false;
        if (TodoAssigneePicker.Value(assignee) == TodoAssigneePicker.ValueFor(item)) return true;
        await RunAsync(() => TodoStore.UpdateAsync(new UpdateTodoItem(
            item.TodoItemId, item.Title, NullIfBlank(item.Notes),
            assignee?.Role, assignee?.PersonEmail, item.DueAt, item.IsComplete)));
        return error is null;
    }

    private async Task<bool> MoveAsync(string destination)
    {
        if (item is null) return false;
        await RunAsync(() => TodoStore.MoveAsync(new MoveTodoItem(
            item.TodoItemId,
            destination == TodoScope.General ? "" : destination)));
        return error is null;
    }

    private Task StartAsync() =>
        RunAsync(() => TodoStore.LogProgressAsync(new LogTodoProgress(TodoItemId, TodoActivityKind.Started)));

    // The timeline panel's log-a-chase form: run the command, answer whether it landed.
    private async Task<bool> LogProgressAsync(TodoActivityKind kind, string? note)
    {
        if (item is null) return false;
        await RunAsync(() => TodoStore.LogProgressAsync(new LogTodoProgress(item.TodoItemId, kind, note)));
        return error is null;
    }

    private async Task DeleteAsync()
    {
        if (item is null || busy) return;
        if (!deleteArmed) { deleteArmed = true; return; }
        deleteArmed = false;
        error = null;
        try
        {
            busy = true;
            await TodoStore.DeleteAsync(item.TodoItemId);
            Nav.NavigateTo("/todos");
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "That action didn't complete. Please try again."; }
        finally { busy = false; }
    }

    // One path for every item-changing command: run it, then re-read the item so the page shows
    // the server's answer rather than a local guess. Repaints explicitly: the Timeline panel's
    // form reaches here through a Func, not an EventCallback, so nothing else would re-render
    // the header pill, the buttons or the error bar.
    private async Task RunAsync(Func<Task> action)
    {
        if (busy) return;
        error = null;
        try
        {
            busy = true;
            StateHasChanged();
            await action();
            item = await TodoStore.GetAsync(TodoItemId);
            activityVersion++;
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "That action didn't complete. Please try again."; }
        finally { busy = false; StateHasChanged(); }
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
