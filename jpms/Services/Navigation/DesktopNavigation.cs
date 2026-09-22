
namespace Jewel.JPMS.Services.Navigation;

/// <summary>
/// The sidebar catalog — the app's single navigation plane. One list of collapsible folders
/// (SidebarFolders, docs/Pathway-Split-Platform-Flow-Plan.md §6) under the project picker, with
/// Home above everything and any folderless rows (SidebarFolders.Standalone — destinations that
/// are not about the picked project) as top-level links at the foot. Folders mix project-scoped
/// rows ({project} templates resolved against CurrentProjectService) with company rows where the
/// work mixes. This class is the RBAC home:
/// the role sets, the per-role folder filtering, and the flatten rule — a role whose whole world
/// is one folder sees rows, never a folder header. Role gates reproduce what each page had
/// before the regrouping; grouping widens nothing, and administrators see everything.
/// </summary>
public static class DesktopNavigation
{
    /// <summary>A folder after role-filtering: only the rows the role can see, only rendered at
    /// all when at least one row survived.</summary>
    public sealed record VisibleFolder(
        SidebarFolder Folder, string Label, string IconKey, IReadOnlyList<NavigationItem> Items);

    public static readonly NavigationItem Home = new("Home", "/dashboard");

    public static bool CanSee(Role role, IReadOnlyList<Role> visibleTo) =>
        role == Role.Admin || visibleTo.Contains(role);

    public static bool CanSeeProjects(Role role) => CanSee(role, NavigationRoles.ProjectRoles);

    /// <summary>Whether the role's visible nav actually contains a project-scoped row — what the
    /// sidebar's project picker gates on. Distinct from CanSeeProjects (the API-mirroring "may
    /// open projects" set): under the nav clamp a PM can still open project pages by URL, but a
    /// picker above an empty rail would be an orphan.</summary>
    public static bool HasProjectScopedRows(Role role) =>
        FoldersFor(role).SelectMany(folder => folder.Items).Any(item => item.IsProjectScoped)
        || StandaloneItemsFor(role).Any(item => item.IsProjectScoped);

    /// <summary>Who may open the assistant chat panel: the commercial team, plus administrators via
    /// the CanSee bypass.</summary>
    public static bool CanUseAssistant(Role role) => CanSee(role, NavigationRoles.AssistantRoles);

    /// <summary>The sidebar's folders for a role: each folder keeps only the rows the role can
    /// see, and a folder with no surviving rows disappears entirely. Built from SidebarFolders
    /// so the sidebar, the landing-page cards and the page-heading matcher can never drift.</summary>
    public static IReadOnlyList<VisibleFolder> FoldersFor(Role role) =>
        SidebarFolders.All
            .Select(folder => new VisibleFolder(
                folder.Folder,
                folder.Label,
                folder.IconKey,
                folder.Rows.Where(row => CanSee(role, row.VisibleTo)).Select(row => row.Item).ToList()))
            .Where(folder => folder.Items.Count > 0)
            .ToList();

    /// <summary>The folderless rows a role may see (SidebarFolders.Standalone), in catalog order.
    /// They render as top-level links at the foot of the sidebar, below every folder; an empty
    /// list renders nothing at all.</summary>
    public static IReadOnlyList<NavigationItem> StandaloneItemsFor(Role role) =>
        SidebarFolders.Standalone
            .Where(row => CanSee(role, row.VisibleTo))
            .Select(row => row.Item)
            .ToList();

    /// <summary>Where the bare project URL (/projects/{id}) lands: the first project-scoped row
    /// of the first visible folder — Project → RFIs for full-access roles. The RFIs
    /// fallback keeps the redirect deterministic if a role somehow reaches a project URL with no
    /// project rows; the page's own RBAC remains the enforcement.</summary>
    public static string FirstProjectTabHref(Role role, string projectId)
    {
        var first = FoldersFor(role)
            .SelectMany(folder => folder.Items)
            .FirstOrDefault(item => item.IsProjectScoped);
        return (first ?? new NavigationItem("RFIs", "/projects/{project}/requests"))
            .ResolveHref(projectId);
    }

    /// <summary>Every navigable item in sidebar order — for flat consumers like the page-heading
    /// matcher. Catalog order puts project templates before most company routes, so the more
    /// specific project routes win where it matters.</summary>
    public static IReadOnlyList<NavigationItem> ItemsVisibleTo(Role role)
    {
        var items = new List<NavigationItem> { Home };
        items.AddRange(FoldersFor(role).SelectMany(folder => folder.Items));
        items.AddRange(StandaloneItemsFor(role));
        return items;
    }
}
