
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
        Role.OfficeAdmin,
        Role.Foreman,
        Role.Accounts
    };

    private static readonly Role[] AllRoles =
        AllInternalRoles
            .Append(Role.Architect)
            .Append(Role.Client)
            .Append(Role.Subcontractor)
            .ToArray();

    // NAV CLAMP (decision 2026-08-11): every sidebar row is currently gated DirectorRoles —
    // only the MD, FD and administrators use the system, so every other role sees Home alone
    // until its own nav is designed. The per-duty sets below are deliberately KEPT: they still
    // mirror the API's authorisation sets (untouched by the clamp) and they are what the future
    // per-role nav will be rebuilt from. Do not delete them for being unreferenced by
    // SidebarFolders.

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
        Role.OfficeComplianceCoordinator,
        // Office Admin mirrors the Compliance role's reach (decision 2026-08-07).
        Role.OfficeAdmin
    };

    // Who sees the master To-do list in the sidebar. The project roles — whose sidebar is the
    // project — plus Accounts, whose whole reason for existing is the to-do list: it is NOT a
    // ProjectRole (no project tabs, no registers), so without its own set the one page it needs
    // would be unreachable. Mirrors the API's ListMyTodoItems floor (JpmsRoleSets.AllInternal)
    // narrowed to the roles that actually carry assignable items.
    internal static readonly Role[] TodoListRoles =
        ProjectRoles.Append(Role.Accounts).ToArray();

    // Who sees the Architect's Instruction register. Mirrors the API's ArchitectInstructionRoles:
    // the project roles that own the commercial consequence of an instruction, plus the architect
    // who issues them (they can file their own rather than emailing and waiting).
    internal static readonly Role[] ArchitectInstructionRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor,
        Role.SiteManager,
        Role.Architect
    };

    internal static readonly Role[] FinanceRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor
    };

    // The people who make routing decisions — mirrors the API's TriageRoles gate. Gates both
    // the Control Centre (formerly Triage) and the Audit Trail (reviewing routing decisions is
    // the same duty).
    // The MD joined when his dashboard grew a triage-backlog tile (RoleHome): a highlight he
    // could not click through was worse than none.
    internal static readonly Role[] TriageRoles =
    {
        Role.ManagingDirector,
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

    // Nobody by role — combined with the CanSee bypass this reads as "administrators only".
    // Used for the Admin folder (user administration): FDs hold the same PERMISSIONS on the API
    // (AdminGate), but the Admin area is the administrator's home turf, deliberately kept off
    // every ordinary role's sidebar — exactly as the old dashboard panels were.
    internal static readonly Role[] AdministratorOnly = Array.Empty<Role>();

    // Directors only. Originally reserved for the company's most sensitive figures (the bank
    // position); since 2026-08-11 also the whole catalog's nav gate — see the NAV CLAMP note.
    internal static readonly Role[] DirectorRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector
    };

    // The Weekly Cashflow row: the directors plus Accounts — the first (deliberate) exception to
    // the nav clamp, decision 2026-08-27. The page is the accountant's working tool (he moves
    // the payment weeks), so hiding it from his rail would leave the one page built FOR him
    // reachable only by URL. Mirrors the API's WeeklyCashflowGates.WeeklyCashflowRoles — keep
    // the two lists in step. The bank-balance line inside the page stays directors-only (it
    // reads the Xero cash summary, whose gate is untouched).
    internal static readonly Role[] WeeklyCashflowRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.Accounts
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

    /// <summary>Whether the role's visible nav actually contains a project-scoped row — what the
    /// sidebar's project picker gates on. Distinct from CanSeeProjects (the API-mirroring "may
    /// open projects" set): under the nav clamp a PM can still open project pages by URL, but a
    /// picker above an empty rail would be an orphan.</summary>
    public static bool HasProjectScopedRows(Role role) =>
        FoldersFor(role).SelectMany(folder => folder.Items).Any(item => item.IsProjectScoped)
        || StandaloneItemsFor(role).Any(item => item.IsProjectScoped);

    // Decision 2026-07-27: widened from DirectorRoles to the commercial team. The assistant now
    // drafts variations from RFI correspondence inside the Create Variation Order Quote dialog
    // (ProjectRequestDetail.razor), and that work belongs to the people who raise variations —
    // VariationRoles.AllowedToManageVariations, i.e. PM and QS as well as the board. A role that
    // can see the button but not the assistant that fills it in is the worst of both.
    //
    // Spend is still gated, just not by role alone: ChatPanel's cost notice is accepted once per
    // user per browser before a single message is sent, and every turn is logged against the
    // sender's name in AgentActivity.
    //
    // Mirrors the API's AiRoles.AllowedToUseAssistant — keep the two lists in step.
    internal static readonly Role[] AssistantRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor
    };

    /// <summary>Who may open the assistant chat panel: the commercial team, plus administrators via
    /// the CanSee bypass.</summary>
    public static bool CanUseAssistant(Role role) => CanSee(role, AssistantRoles);

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
