using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Features.Todos;

namespace Jewel.JPMS.Pages;

public partial class Todos
{
    private sealed record ScopeProject(string ProjectId, string Label, string Name);

    private IReadOnlyList<Project> KnownProjects => Projects.Current ?? Array.Empty<Project>();

    // Only projects that actually have items on the current list appear in the scope filter.
    private IReadOnlyList<ScopeProject> ProjectsWithItems =>
        items
            .Where(item => !item.IsGeneral && item.ProjectLabel is not null)
            .GroupBy(item => item.ProjectId)
            .Select(group => group.First())
            .Select(item => new ScopeProject(item.ProjectId, item.ProjectLabel!, item.ProjectName!))
            .ToList();

    private ScopeProject? ScopedProject =>
        scopeFilter is ScopeAll or ScopeGeneral
            ? null
            : ProjectsWithItems.FirstOrDefault(project => project.ProjectId == scopeFilter);

    // Every project the reader can see, in the one order the app uses for projects (live work
    // first) — ListProjectsVisibleToUserHandler has already applied it, and nothing here narrows
    // the list, so it is not re-sorted. Not ProjectsWithItems: the point is adding the first item
    // to a project that hasn't got one yet.
    private IReadOnlyList<SearchSelect.Option> ProjectOptions =>
        KnownProjects
            .Select(p => new SearchSelect.Option(p.ProjectId, $"{p.Reference} — {p.Name}"))
            .ToList();

    // The blank row's label — and so the empty control's placeholder — says what leaving it blank
    // MEANS, which differs by gate: company-wide for the MD/administrators, nothing at all for
    // everyone else.
    private string ProjectPickerPlaceholder =>
        !filtersReady ? "Loading…"
        // With no projects to pick, blank is the only answer left — and for anyone but the MD it
        // is a refused one, so the control says why rather than looking merely empty.
        : !CanListProjects ? "Your role can't pick a project here"
        : projectsFailed ? "Couldn't load projects — reload to try again"
        : CanSeeAll ? "Company-wide — no project"
        : "Pick a project";

    private string NewProjectLabel =>
        ProjectOptions.FirstOrDefault(option => option.Value == newProject)?.Label ?? "the project";

    private static bool IsGeneral(TodoItem item) => item.IsGeneral;

    // "Mine" = assigned to a role the signed-in user holds — the same rule the API's
    // UpdateTodoItemAuthorisation applies for the tick-off path. An item pinned to a DIFFERENT
    // person is theirs, not this reader's, even inside the same role.
    private bool IsMine(TodoItem item) =>
        item.AssigneeRole is Role role && Session.AvailableRoles.Contains(role)
        && (item.AssigneePersonEmail is null
            || string.Equals(item.AssigneePersonEmail, Auth.CurrentUser?.Email, StringComparison.OrdinalIgnoreCase));

    private static string ScopeLabel(TodoItem item) =>
        item.IsGeneral ? "Company-wide" : item.ProjectLabel ?? "Project";

    // The board card's project badge: reference AND name (the ref alone said nothing to anyone
    // not living in the numbering; the board truncates long names, hover for the full label), or
    // the accent "Company" chip for a no-project item.
    private static TodoBoard.ScopeChip ScopeChipFor(TodoItem item)
    {
        if (item.IsGeneral) return new TodoBoard.ScopeChip("Company", "Company-wide — no project", true);
        var label = item.ProjectLabel ?? "Project";
        return new TodoBoard.ScopeChip(label, label, false);
    }
}
