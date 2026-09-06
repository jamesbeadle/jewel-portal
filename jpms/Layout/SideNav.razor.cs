using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Layout;

public partial class SideNav
{
    [Parameter] public EventCallback OnNavigate { get; set; }
    [Parameter] public bool IsExpanded { get; set; } = true;
    [Parameter] public EventCallback OnToggleExpand { get; set; }

    private bool pickerOpen;
    private bool projectsRequested;

    // Explicit folder toggles, remembered for the browser session. Static on purpose: it
    // survives layout re-creation and navigation without any new storage plumbing, and losing
    // it on a full reload is fine for a per-session preference. Folders the user has never
    // toggled follow the route instead — see IsFolderOpen.
    private static readonly Dictionary<SidebarFolder, bool> folderToggles = new();

    private string LabelClass => IsExpanded ? "hidden md:inline truncate" : "hidden";

    private string HeaderLayout => IsExpanded
        ? "h-14 px-2 flex items-center justify-center md:justify-between"
        : "px-2 py-3 flex flex-col items-center gap-2";

    private string CurrentPath => new Uri(Nav.Uri).AbsolutePath;

    // The picker only earns its space when the role's visible nav actually holds a project-scoped
    // row to retarget — under the directors-only nav clamp (2026-08-11) that is the honest test,
    // where "may open projects" (CanSeeProjects) would hang a picker over an empty rail.
    private bool ShowsProjectPicker =>
        Session.ActiveRole is { } role && DesktopNavigation.HasProjectScopedRows(role);

    // Rows that belong to no folder (SidebarFolders.Standalone) — rendered at the foot of the nav,
    // below every folder, in both the expanded panel and the collapsed rail.
    private IReadOnlyList<NavigationItem> StandaloneItems =>
        Session.ActiveRole is { } role ? DesktopNavigation.StandaloneItemsFor(role) : Array.Empty<NavigationItem>();

    private IReadOnlyList<DesktopNavigation.VisibleFolder> Folders =>
        Session.ActiveRole is { } role ? DesktopNavigation.FoldersFor(role) : Array.Empty<DesktopNavigation.VisibleFolder>();

    // The flatten rule (decision 2026-07-22): one visible folder → its rows render as top-level
    // items with no folder header. Internal staff always span several folders, so they see
    // headers as normal.
    private bool RendersFlat => Folders.Count == 1;

    // Active (non-Completed) projects in the canonical work order — live sites first, then Defects
    // Period, then Leads (ProjectOrdering.InWorkOrder), so the switcher opens on what is on site.
    private List<Project> ActiveProjects =>
        (Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>())
            .Where(project => project.Stage != ProjectStage.Completed)
            .InWorkOrder()
            .ToList();

    // The picker's Completed group, listed after the active projects when the "Show completed"
    // toggle (ProjectStageFilter) is on — same canonical order within the band.
    private List<Project> CompletedProjects =>
        (Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>())
            .Where(project => project.Stage == ProjectStage.Completed)
            .InWorkOrder()
            .ToList();

    // The pool the picker can currently reach — what Recent and ResolveFor validate against.
    private List<Project> PickableProjects =>
        StageFilter.IncludeCompleted
            ? ActiveProjects.Concat(CompletedProjects).ToList()
            : ActiveProjects;

    // The picker's Recent group: the last few projects this user actually opened
    // (CurrentProjectService), most recently opened first — the one place recency, not
    // ProjectOrdering, sets the order, because "what was I just in?" is the question the group
    // answers. Pickable projects only (completed ones join when the toggle is on), without the
    // current one (it is the picker button itself).
    // Deliberately NOT removed from the full list below: that list stays complete and in the
    // canonical work order, so every project keeps one predictable place to be found.
    private List<Project> RecentlyOpened
    {
        get
        {
            var pool = PickableProjects;
            return CurrentProject.RecentProjectIds
                .Where(id => !string.Equals(id, EffectiveProjectId, StringComparison.OrdinalIgnoreCase))
                .Select(id => pool.Find(project =>
                    string.Equals(project.ProjectId, id, StringComparison.OrdinalIgnoreCase)))
                .OfType<Project>()
                .Take(4)
                .ToList();
        }
    }


    private bool IsCurrent(Project candidate) =>
        string.Equals(candidate.ProjectId, EffectiveProjectId, StringComparison.OrdinalIgnoreCase);

    // The workspace's project: last viewed while still in the pickable pool, else the first
    // project of that pool (the "Show completed" toggle widens it to completed projects).
    private string? EffectiveProjectId =>
        CurrentProject.ResolveFor(Projects.Current, StageFilter.IncludeCompleted);

    private Project? PickedProject =>
        EffectiveProjectId is { } id ? Projects.Find(id) : null;

    // A project-scoped row with no project to resolve against renders muted, not dead-linked.
    private bool IsUnresolvable(NavigationItem item) =>
        item.IsProjectScoped && EffectiveProjectId is null;

    private bool IsFolderActive(DesktopNavigation.VisibleFolder folder) =>
        folder.Items.Any(item => item.IsActiveFor(CurrentPath));

    // Never-toggled folders track the route: the folder owning the current page starts (and
    // stays) expanded as the user navigates, the rest stay collapsed. An explicit toggle wins
    // over that default for the rest of the session.
    private bool IsFolderOpen(DesktopNavigation.VisibleFolder folder) =>
        folderToggles.TryGetValue(folder.Folder, out var open) ? open : IsFolderActive(folder);

    private void ToggleFolder(DesktopNavigation.VisibleFolder folder) =>
        folderToggles[folder.Folder] = !IsFolderOpen(folder);

    private async Task PickProject(Project project)
    {
        pickerOpen = false;
        await CurrentProject.RememberAsync(project.ProjectId);
        // On a project page, follow the pick to the same page of the new project; on Home or a
        // company page, just retarget the workspace links and stay put.
        var path = CurrentPath;
        const string prefix = "/projects/";
        if (path.StartsWith(prefix, StringComparison.Ordinal))
        {
            var rest = path[prefix.Length..];
            var slash = rest.IndexOf('/');
            var suffix = slash < 0 ? "" : rest[slash..];
            Nav.NavigateTo($"/projects/{project.ProjectId}{suffix}");
        }
        await HandleNavigate();
    }

    private Task ClosePickerAndNavigate()
    {
        pickerOpen = false;
        return HandleNavigate();
    }

    private string HomeHref =>
        Session.ActiveRole is null ? "/dashboard" : NavigationCatalog.HomeRouteFor(Session.ActiveRole.Value);

    protected override void OnInitialized()
    {
        Session.OnChange += HandleSessionChange;
        Projects.OnChanged += StateHasChanged;
        CurrentProject.OnChange += StateHasChanged;
        StageFilter.OnChange += StateHasChanged;
        Nav.LocationChanged += HandleLocationChanged;
        _ = CurrentProject.EnsureLoadedAsync();
        _ = StageFilter.EnsureLoadedAsync();
        EnsureProjectsLoaded();
    }

    private void HandleSessionChange()
    {
        EnsureProjectsLoaded();
        StateHasChanged();
    }

    // The sidebar resolves project-scoped hrefs (and the picker's label) itself, so it needs the
    // project list even before any project page has loaded it. Fetch once, in the background,
    // only for signed-in approved users with a role that can see projects.
    private void EnsureProjectsLoaded()
    {
        if (projectsRequested || !Session.IsApproved || Projects.Current is not null) return;
        if (!ShowsProjectPicker) return;
        projectsRequested = true;
        _ = RefreshProjectsAsync();
    }

    private async Task RefreshProjectsAsync()
    {
        try { await Projects.RefreshAsync(CancellationToken.None); }
        catch { } // Workspace links fall back to the portfolio; pages retry their own loads.
    }

    private void HandleLocationChanged(object? sender, EventArgs args)
    {
        pickerOpen = false;
        StateHasChanged();
    }

    private string LinkClass(bool isActive)
    {
        var layout = IsExpanded ? "md:justify-start" : "";
        var baseClass = $"flex items-center gap-3 justify-center {layout} px-3 py-2 rounded text-sm transition";
        if (isActive) return $"{baseClass} text-content font-semibold";
        return $"{baseClass} text-content-subtle font-medium hover:text-content hover:bg-surface-raised";
    }

    // Muted twin of LinkClass for unresolvable top-level rows (flat rendering only).
    private string DisabledLinkClass
    {
        get
        {
            var layout = IsExpanded ? "md:justify-start" : "";
            return $"flex items-center gap-3 justify-center {layout} px-3 py-2 rounded text-sm text-content-subtle font-medium opacity-50 cursor-default";
        }
    }

    // Folder rows indent under their header; no icons — the header carries the group.
    private string FolderItemClass(bool isActive)
    {
        var baseClass = "block pl-7 pr-3 py-1.5 rounded text-sm transition truncate";
        if (isActive) return $"{baseClass} text-content font-semibold";
        return $"{baseClass} text-content-subtle hover:text-content hover:bg-surface-raised";
    }

    private string DisabledRowClass =>
        "block pl-7 pr-3 py-1.5 rounded text-sm text-content-subtle opacity-50 cursor-default truncate";

    private Task HandleNavigate() => OnNavigate.InvokeAsync();

    private void SignOut() => Nav.NavigateTo("/logout", forceLoad: true);

    public void Dispose()
    {
        Session.OnChange -= HandleSessionChange;
        Projects.OnChanged -= StateHasChanged;
        CurrentProject.OnChange -= StateHasChanged;
        StageFilter.OnChange -= StateHasChanged;
        Nav.LocationChanged -= HandleLocationChanged;
    }
}
