using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services.Navigation;

/// <summary>
/// The sidebar catalog — the app's single navigation plane. One list of five collapsible folders
/// (SidebarFolders, docs/Pathway-Split-Platform-Flow-Plan.md §6) under the project picker, with
/// Home above everything. Folders mix project-scoped rows ({project} templates resolved against
/// CurrentProjectService) with company rows where the work mixes. This class is the RBAC home:
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

    // Mirrored by the API's JpmsRoleSets.AllInternal — keep the two lists in step.
    private static readonly Role[] AllInternalRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor,
        Role.SiteManager,
        Role.HealthSafetyOfficer,
        Role.OfficeComplianceCoordinator,
        Role.Foreman
    };

    private static readonly Role[] AllRoles =
        AllInternalRoles
            .Append(Role.Architect)
            .Append(Role.Client)
            .Append(Role.Subcontractor)
            .ToArray();

    // The internal office/management roles that can open projects. Internal (not public):
    // SidebarFolders is the only outside consumer, and it lives in this assembly.
    internal static readonly Role[] ProjectRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor,
        Role.SiteManager,
        Role.HealthSafetyOfficer,
        Role.OfficeComplianceCoordinator
    };

    internal static readonly Role[] FinanceRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor
    };

    // The people who make routing decisions — mirrors the API's TriageRoles gate. Gates both
    // the Triage queue and the Audit Trail (reviewing routing decisions is the same duty).
    internal static readonly Role[] TriageRoles =
    {
        Role.ProjectManager,
        Role.FinanceDirector
    };

    // Mirrors the API's labour registry authorisation (LabourRoleSets.ManageWorkers).
    internal static readonly Role[] WorkerRegistryRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager
    };

    // Directors only — reserved for the company's most sensitive figures (Cash Summary).
    internal static readonly Role[] DirectorRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector
    };

    // Decision 2026-07-22: widened from MD-only so the merged Directory page keeps the old
    // Clients/Architects reach for PMs and adds the FD (Admin included via the CanSee bypass).
    internal static readonly Role[] DirectoryRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager
    };

    public static readonly NavigationItem Home = new("Home", "/dashboard");

    public static bool CanSee(Role role, IReadOnlyList<Role> visibleTo) =>
        role == Role.Admin || visibleTo.Contains(role);

    public static bool CanSeeProjects(Role role) => CanSee(role, ProjectRoles);

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

    /// <summary>Where the bare project URL (/projects/{id}) lands: the first project-scoped row
    /// of the first visible folder — Client → Requests for full-access roles. The Requests
    /// fallback keeps the redirect deterministic if a role somehow reaches a project URL with no
    /// project rows; the page's own RBAC remains the enforcement.</summary>
    public static string FirstProjectTabHref(Role role, string projectId)
    {
        var first = FoldersFor(role)
            .SelectMany(folder => folder.Items)
            .FirstOrDefault(item => item.IsProjectScoped);
        return (first ?? new NavigationItem("Requests", "/projects/{project}/requests"))
            .ResolveHref(projectId);
    }

    /// <summary>Every navigable item in sidebar order — for flat consumers like the page-heading
    /// matcher. Catalog order puts project templates before most company routes, so the more
    /// specific project routes win where it matters.</summary>
    public static IReadOnlyList<NavigationItem> ItemsVisibleTo(Role role)
    {
        var items = new List<NavigationItem> { Home };
        items.AddRange(FoldersFor(role).SelectMany(folder => folder.Items));
        return items;
    }
}
