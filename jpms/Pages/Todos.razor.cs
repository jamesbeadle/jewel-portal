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
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Todos;

namespace Jewel.JPMS.Pages;

public partial class Todos
{
    private const string ScopeAll = "__all__";
    private const string ScopeGeneral = "__general__";
    private const string Unassigned = "__unassigned__";

    private enum StatusFilter { Open, Done, All }

    // Session checked and the user signed in. This is NOT "the data is here" — keeping the two
    // apart is what lets the page show its chrome at once and hold the list until it lands.
    private bool sessionReady;
    private bool loading = true;
    // A failed fetch has to open the gate, or the jewel pulses forever; the panel then says so
    // rather than reporting an empty list nobody actually asked for.
    private bool listFailed;
    // The filter row is built from the same round of fetches as the list, so it waits with it.
    private bool filtersReady;
    // The project list failed to load — the scope filter loses its labels and the add modal's
    // project picker has nothing to offer, so it says that instead of pretending to be empty.
    private bool projectsFailed;
    private bool busy;
    private string? error;

    private IReadOnlyList<TodoItem> items = Array.Empty<TodoItem>();
    // The flattened role/"Role — Person" pool feeds the add modal's assignee picker; the filter
    // row reads the raw lists below instead.
    private IReadOnlyList<SearchSelect.Option> assigneeOptions = Array.Empty<SearchSelect.Option>();
    private IReadOnlyList<Role> assignableRoles = Array.Empty<Role>();
    private IReadOnlyList<TodoAssignablePerson> assignablePeople = Array.Empty<TodoAssignablePerson>();

    private StatusFilter statusFilter = StatusFilter.Open;
    // Keyword search (the project tab's idiom): filters the to-dos client-side and drives the
    // tagged-email lookup. While a query is live the status tabs are bypassed — search looks
    // across Open and Done alike, so a completed item is always findable.
    private string search = "";
    // Board (the default) or flat list — read from TodoViewStorage per user, written back on
    // toggle, and shared with the project tab and the dashboard panel.
    private bool boardView = true;
    private string scopeFilter = ScopeAll;
    // The two assignee filters: a role (its int as a string, or Unassigned, "" = any) and a
    // person's email ("" = anyone). Independent — see MatchesAssigneeFilter.
    private string roleFilter = "";
    private string personFilter = "";

    // "Add a to-do" modal state (the manage gate — see CanManage). newProject is the picked
    // project's id, "" = company-wide, which only CanSeeAll may post.
    private bool addOpen;
    private string? addError;
    private string newProject = "";
    private string newTitle = "";
    private string newNotes = "";
    private string newAssignee = "";
    private string newDue = "";

    // Said after an add whose item did NOT come back on the reader's own list — it went to another
    // role, so without this it would look like nothing happened. Cleared on the next add.
    private string? addedNote;
    private string? addedNoteHref;

    // Mirrors the API's TodoRoles.AllowedToSeeAllTodos gate: the MD and administrators browse
    // everything; everyone else reads their own list (ListMyTodoItems).
    private bool CanSeeAll =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector);

    // Mirrors the API's TodoRoles.AllowedToManageTodos gate — deliberately WIDER than CanSeeAll:
    // an FD, PM, site manager or accounts user reads only their own items here but may still hand
    // one on to another role, exactly as they can on a project's To-do tab.
    private bool CanManage =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector
            or Role.ProjectManager or Role.SiteManager or Role.Accounts);

    // Mirrors the API's JpmsRoleSets.AllInternal — who may read to-dos at all.
    private bool HasInternalRole =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector
            or Role.ProjectManager or Role.QuantitySurveyor or Role.SiteManager or Role.HealthSafetyOfficer
            or Role.OfficeComplianceCoordinator or Role.OfficeAdmin or Role.Foreman or Role.Accounts);

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        sessionReady = true;
        boardView = await ViewStorage.ReadBoardAsync(Auth.CurrentUser!.Email);
        // Paint the chrome before the fetches: Blazor re-renders OnInitializedAsync only at its
        // FIRST await, which has already passed, so without this the page waits on the list.
        StateHasChanged();
        if (!Session.IsApproved || !HasInternalRole) { loading = false; return; }

        // The list, the project labels and the assignable-role pool are independent, so they go
        // out together rather than one after another.
        var loads = new List<Task> { LoadAsync(), LoadProjectLabelsAsync() };
        // The role pool feeds the assignee FILTER (MD/admin only) and the add modal's assignee
        // picker (anyone in the manage gate), so either reason is enough to fetch it.
        if (CanSeeAll || CanManage) loads.Add(LoadAssigneeOptionsAsync());
        await Task.WhenAll(loads);
        filtersReady = true;
    }

    // Project labels for the scope chips/filter, and the assignable-role pool for the
    // picker/filter. Either failing degrades the labels, never the list itself.
    private async Task LoadProjectLabelsAsync()
    {
        try { if (Projects.Current is null) await Projects.RefreshAsync(CancellationToken.None); }
        // Degrades the scope filter's labels — and, since the add modal picks a project from this
        // same list, leaves nothing to pick. The picker says so rather than sitting empty.
        catch { projectsFailed = true; }
    }

    private async Task LoadAssigneeOptionsAsync()
    {
        try
        {
            var rolesTask = TodoStore.ListAssignableRolesAsync();
            var peopleTask = TodoStore.ListAssignablePeopleAsync();
            assignableRoles = await rolesTask;
            assignablePeople = await peopleTask;
            assigneeOptions = TodoAssigneePicker.BuildOptions(assignableRoles, assignablePeople);
        }
        catch { }
    }

    private async Task LoadAsync()
    {
        loading = true;
        try { items = CanSeeAll ? await TodoStore.ListAllAsync() : await TodoStore.ListMineAsync(); listFailed = false; }
        catch { error = "Couldn't load the to-do list. Please try again."; listFailed = true; }
        finally { loading = false; }
    }

    // Everything the scope and assignee filters — and the search — leave standing: the BOARD's
    // item set (it shows both statuses as columns, so the status filter is not applied here).
    private IReadOnlyList<TodoItem> FilteredItems =>
        items
            .Where(item => scopeFilter switch
            {
                ScopeAll => true,
                ScopeGeneral => IsGeneral(item),
                _ => item.ProjectId == scopeFilter
            })
            .Where(item => MatchesAssigneeFilter(item))
            .Where(item => !HasQuery || MatchesTodo(item))
            .ToList();

    // The LIST's item set: the filtered items narrowed further by the Open/Done/All tabs — except
    // while a query is live, when the search looks across every status (the tabs render disabled).
    private IReadOnlyList<TodoItem> VisibleItems =>
        FilteredItems
            .Where(item => HasQuery || statusFilter switch
            {
                StatusFilter.Open => !item.IsComplete,
                StatusFilter.Done => item.IsComplete,
                _ => true
            })
            .ToList();

    // ---- Keyword search (same rules as ProjectTodoList's) --------------------------------------

    private bool HasQuery => !string.IsNullOrWhiteSpace(search);

    private void OnSearchInput(ChangeEventArgs e) => search = e.Value?.ToString() ?? "";

    private void ClearSearch() => search = "";

    // Every whitespace-separated keyword must appear in at least one of the item's fields
    // (case-insensitive) — AND across keywords so extra words narrow the results. "todo-0011"
    // matches the Reference; a pinned assignee's name matches too.
    private bool MatchesKeywords(params string?[] fields)
    {
        var tokens = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.All(token =>
            fields.Any(field => field is not null && field.Contains(token, StringComparison.OrdinalIgnoreCase)));
    }

    private bool MatchesTodo(TodoItem item) =>
        MatchesKeywords(item.Reference, item.Title, item.Notes, item.AssigneePersonName);

    // Matches the search but not the scope/assignee filters — announced under the list so a
    // filtered-out hit never reads as "not found".
    private int HiddenMatchCount =>
        !HasQuery ? 0 : items.Count(MatchesTodo) - (boardView ? FilteredItems : VisibleItems).Count;

    // --------------------------------------------------------------------------------------------

    // Role and person are independent narrowings: a role alone matches every item assigned to it
    // — pinned or not — a person alone matches everything pinned to them under ANY role they
    // hold, and both together intersect. Unassigned means the role slot is empty, so no person
    // filter can apply with it (the select is disabled and cleared while it's picked).
    private bool MatchesAssigneeFilter(TodoItem item)
    {
        if (roleFilter == Unassigned) return item.AssigneeRole is null;
        if (SelectedRoleFilter is Role role && item.AssigneeRole != role) return false;
        return personFilter == ""
            || string.Equals(item.AssigneePersonEmail, personFilter, StringComparison.OrdinalIgnoreCase);
    }

    private Role? SelectedRoleFilter =>
        roleFilter != "" && roleFilter != Unassigned && int.TryParse(roleFilter, out var value)
            ? (Role)value
            : null;

    // The person filter's pool: holders of the selected role, or — with no role picked — every
    // assignable user once (a person holding two roles is still one person), A–Z by name.
    private IReadOnlyList<TodoAssignablePerson> FilterablePeople =>
        SelectedRoleFilter is Role role
            ? assignablePeople.Where(person => person.Role == role).ToList()
            : assignablePeople
                .GroupBy(person => person.Email, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(person => person.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

    private void OnRoleFilterChange(ChangeEventArgs e)
    {
        roleFilter = e.Value?.ToString() ?? "";
        // An unassigned item has no person, and a person picked under the old role may not hold
        // the new one — either way the person filter resets rather than silently matching nothing.
        if (personFilter != ""
            && (roleFilter == Unassigned
                || !FilterablePeople.Any(person => string.Equals(person.Email, personFilter, StringComparison.OrdinalIgnoreCase))))
            personFilter = "";
    }

    // Only projects that actually have items on the current list appear in the scope filter.
    private IReadOnlyList<Project> ProjectsWithItems
    {
        get
        {
            var projectIds = items.Select(i => i.ProjectId).Where(id => !string.IsNullOrWhiteSpace(id)).ToHashSet();
            return (Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>())
                .Where(p => projectIds.Contains(p.ProjectId))
                .ToList();
        }
    }

    // The project the scope filter is narrowed to, once its label is known — null for All and
    // Company-wide, and while the project list is still in flight.
    private Project? ScopedProject =>
        scopeFilter is ScopeAll or ScopeGeneral
            ? null
            : (Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>())
                .FirstOrDefault(p => p.ProjectId == scopeFilter);

    // Every project the reader can see, in the one order the app uses for projects (live work
    // first) — ListProjectsVisibleToUserHandler has already applied it, and nothing here narrows
    // the list, so it is not re-sorted. Not ProjectsWithItems: the point is adding the first item
    // to a project that hasn't got one yet.
    private IReadOnlyList<SearchSelect.Option> ProjectOptions =>
        (Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>())
            .Select(p => new SearchSelect.Option(p.ProjectId, $"{p.Reference} — {p.Name}"))
            .ToList();

    // The blank row's label — and so the empty control's placeholder — says what leaving it blank
    // MEANS, which differs by gate: company-wide for the MD/administrators, nothing at all for
    // everyone else.
    private string ProjectPickerPlaceholder =>
        !filtersReady ? "Loading…"
        // With no projects to pick, blank is the only answer left — and for anyone but the MD it
        // is a refused one, so the control says why rather than looking merely empty.
        : projectsFailed ? "Couldn't load projects — reload to try again"
        : CanSeeAll ? "Company-wide — no project"
        : "Pick a project";

    private string NewProjectLabel =>
        ProjectOptions.FirstOrDefault(option => option.Value == newProject)?.Label ?? "the project";

    private static bool IsGeneral(TodoItem item) => string.IsNullOrWhiteSpace(item.ProjectId);

    // "Mine" = assigned to a role the signed-in user holds — the same rule the API's
    // UpdateTodoItemAuthorisation applies for the tick-off path. An item pinned to a DIFFERENT
    // person is theirs, not this reader's, even inside the same role.
    private bool IsMine(TodoItem item) =>
        item.AssigneeRole is Role role && Session.AvailableRoles.Contains(role)
        && (item.AssigneePersonEmail is null
            || string.Equals(item.AssigneePersonEmail, Auth.CurrentUser?.Email, StringComparison.OrdinalIgnoreCase));

    private string ScopeLabel(TodoItem item)
    {
        if (IsGeneral(item)) return "Company-wide";
        var project = (Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>())
            .FirstOrDefault(p => p.ProjectId == item.ProjectId);
        return project is null ? "Project" : $"{project.Reference} — {project.Name}";
    }

    // The board card's project badge: reference AND name (the ref alone said nothing to anyone
    // not living in the numbering; the board truncates long names, hover for the full label), or
    // the accent "Company" chip for a no-project item. While the project labels are still in
    // flight the chip says just "Project" rather than nothing.
    private TodoBoard.ScopeChip ScopeChipFor(TodoItem item)
    {
        if (IsGeneral(item)) return new TodoBoard.ScopeChip("Company", "Company-wide — no project", true);
        var project = (Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>())
            .FirstOrDefault(p => p.ProjectId == item.ProjectId);
        return project is null
            ? new TodoBoard.ScopeChip("Project", "Project", false)
            : new TodoBoard.ScopeChip($"{project.Reference} — {project.Name}", $"{project.Reference} — {project.Name}", false);
    }

    // The role filter's option pool: the roles that actually HAVE items on this reader's current
    // list, A–Z by display name. Unassigned is offered when it would match something (or to the
    // MD/administrators, whose list is the master one).
    private IReadOnlyList<Role> RolesOnList =>
        items.Where(item => item.AssigneeRole is not null)
            .Select(item => item.AssigneeRole!.Value)
            .Distinct()
            .OrderBy(role => role.DisplayName(), StringComparer.OrdinalIgnoreCase)
            .ToList();

    private bool HasUnassigned => items.Any(item => item.AssigneeRole is null);

    private string StatusTabClass(StatusFilter tab) =>
        statusFilter == tab
            ? "btn-primary text-xs px-2.5 py-1.5"
            : "btn-secondary text-xs px-2.5 py-1.5";

    private string ViewTabClass(bool board) =>
        boardView == board
            ? "btn-primary text-xs px-2.5 py-1.5"
            : "btn-secondary text-xs px-2.5 py-1.5";

    private async Task SetView(bool board)
    {
        if (boardView == board) return;
        boardView = board;
        await ViewStorage.WriteAsync(Auth.CurrentUser!.Email, board);
    }

    private void OnNewNotesInput(ChangeEventArgs e) => newNotes = e.Value?.ToString() ?? "";

    private void OpenAdd()
    {
        newTitle = newNotes = newAssignee = newDue = "";
        // The scope filter is the reader's stated context: raising an item while one project is
        // picked means an item on that project. All / Company-wide leave the picker to say.
        newProject = scopeFilter is ScopeAll or ScopeGeneral ? "" : scopeFilter;
        addError = null;
        addedNote = addedNoteHref = null;
        addOpen = true;
    }

    private void CloseAdd()
    {
        addOpen = false;
        addError = null;
    }

    private async Task Add()
    {
        if (busy) return;
        if (string.IsNullOrWhiteSpace(newTitle)) { addError = "A title is required."; return; }
        // Blank = company-wide, which is the managing director's / administrators' call. Said here
        // rather than by hiding the blank row, so the answer is "not yours to make", not "missing".
        if (newProject == "" && !CanSeeAll)
        {
            addError = "Pick the project this item belongs to — only the managing director adds company-wide items.";
            return;
        }
        addError = null;
        addedNote = addedNoteHref = null;
        var assignee = TodoAssigneePicker.Parse(newAssignee);
        var title = newTitle.Trim();
        var notes = NullIfBlank(newNotes);
        var due = ParseDate(newDue);
        // Held for the "added, but not on your list" note below — Run reloads the list around it.
        TodoItem? added = null;
        await Run(async () =>
        {
            added = newProject == ""
                ? await TodoStore.AddGeneralAsync(new AddGeneralTodoItem(
                    title, notes, assignee?.Role, assignee?.PersonEmail, due))
                : await TodoStore.AddAsync(new AddTodoItem(
                    newProject, title, notes, assignee?.Role, assignee?.PersonEmail, due));
            CloseAdd();
        });
        // A failed add leaves the modal open with the page-level error mirrored inside it. A
        // failure AFTER the add — the reload — has already closed the modal; the red bar carries
        // that one, and the stale list it leaves behind is no basis for the note below.
        if (error is not null)
        {
            if (addOpen) addError = error;
            return;
        }
        // On a CanSeeAll list every added item comes back, so this never fires there. On a reader's
        // own list an item assigned to someone else's role does not — say where it went instead.
        // Tested against what the reader can SEE, not what came back: an item raised for Ravenswood
        // while the filter says Company-wide (or while Done is the open tab) is on the list and
        // still invisible, which looks exactly like the add having done nothing.
        if (added is TodoItem raised
            && !(boardView ? FilteredItems : VisibleItems).Any(item => item.TodoItemId == raised.TodoItemId))
        {
            addedNote = items.Any(item => item.TodoItemId == raised.TodoItemId)
                ? $"{raised.Reference} added to {ScopeLabel(raised)}. These filters are hiding it."
                : $"{raised.Reference} added to {ScopeLabel(raised)}. It's assigned to another role, so it isn't on your own list.";
            addedNoteHref = string.IsNullOrWhiteSpace(raised.ProjectId) ? null : $"/projects/{raised.ProjectId}/todos";
        }
    }

    // Opening an item is a navigation: everything beyond the row — full detail, linked to-dos,
    // communications, the manage actions — lives on the item's own page.
    private void OpenItem(TodoItem item) => Nav.NavigateTo($"/todos/{item.TodoItemId}");

    // The board's drag onto the other column — this page's one in-place status change. The
    // reloaded list re-files the item under its new status with the server's ordering.
    private async Task SetComplete(TodoItem item, bool complete)
    {
        if (busy || item.IsComplete == complete) return;
        await Run(() => TodoStore.UpdateAsync(new UpdateTodoItem(
            item.TodoItemId,
            item.Title,
            NullIfBlank(item.Notes),
            item.AssigneeRole,
            item.AssigneePersonEmail,
            item.DueAt,
            complete)));
    }

    private async Task Run(Func<Task> action)
    {
        error = null;
        try
        {
            busy = true;
            await action();
            // Take the server's fresh open-first/done-pile order: a dragged card re-filing on
            // reload is the expected "moves to the done column" behaviour.
            items = CanSeeAll ? await TodoStore.ListAllAsync() : await TodoStore.ListMineAsync();
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "That action didn't complete. Please try again."; }
        finally { busy = false; }
    }

    private static bool IsOverdue(TodoItem item) =>
        !item.IsComplete && item.DueAt is not null && item.DueAt.Value < DateTimeOffset.Now.Date;

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTimeOffset? ParseDate(string value) =>
        DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
}
