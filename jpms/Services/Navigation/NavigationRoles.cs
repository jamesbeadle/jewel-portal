
namespace Jewel.JPMS.Services.Navigation;

/// <summary>
/// The role sets the sidebar catalog is gated by — the RBAC vocabulary of the nav, one named set
/// per duty. Each mirrors the API gate of the pages its rows land on (a row is never shown to a
/// role its page would refuse) and then narrows to the role's own job; the comments name the API
/// set each one keeps in step with. DesktopNavigation applies them (CanSee), SidebarFolders
/// assigns them row by row.
/// </summary>
public static class NavigationRoles
{
    // Mirrored by the API's JpmsRoleSets.AllInternal — keep the two lists in step.
    public static readonly Role[] AllInternalRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor,
        Role.SiteManager,
        Role.HealthSafetyOfficer,
        Role.OfficeComplianceCoordinator,
        Role.OfficeAdmin,
        Role.SalesMarketing,
        Role.Foreman,
        Role.Accounts
    };

    // The per-role nav (2026-09-22, Nigel: "review each role and build the appropriate homepage
    // dashboard and side nav for their role") replaced the 2026-08-11 clamp that showed every
    // row to the directors alone. The hard line is external logins: an architect sees the four
    // project rows their scoped reads admit, and clients and subcontractors see Home alone (their
    // own portal pages). Administrators bypass every set (DesktopNavigation.CanSee).

    // The internal office/management roles that can open projects.
    public static readonly Role[] ProjectRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor,
        Role.SiteManager,
        Role.HealthSafetyOfficer,
        Role.OfficeComplianceCoordinator,
        // Office Admin mirrors the Compliance role's reach (decision 2026-08-07); Sales &
        // Marketing mirrors Office Admin (decision 2026-09-03).
        Role.OfficeAdmin,
        Role.SalesMarketing
    };

    // Who sees the master To-do list in the sidebar. The project roles — whose sidebar is the
    // project — plus Accounts, whose whole reason for existing is the to-do list: it is NOT a
    // ProjectRole (no project tabs, no registers), so without its own set the one page it needs
    // would be unreachable. Mirrors the API's ListMyTodoItems floor (JpmsRoleSets.AllInternal)
    // narrowed to the roles that actually carry assignable items.
    public static readonly Role[] TodoListRoles =
        ProjectRoles.Append(Role.Accounts).ToArray();

    // Who sees the Architect's Instruction register. Mirrors the API's ArchitectInstructionRoles:
    // the project roles that own the commercial consequence of an instruction, plus the architect
    // who issues them (they can file their own rather than emailing and waiting).
    public static readonly Role[] ArchitectInstructionRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor,
        Role.SiteManager,
        Role.Architect
    };

    public static readonly Role[] FinanceRoles =
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
    public static readonly Role[] TriageRoles =
    {
        Role.ManagingDirector,
        Role.ProjectManager,
        Role.FinanceDirector
    };

    // Mirrors the API's labour registry authorisation (LabourRoleSets.ManageWorkers).
    public static readonly Role[] WorkerRegistryRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager
    };

    // Nobody by role — combined with the CanSee bypass this reads as "administrators only".
    // Used for the Admin folder (user administration): FDs hold the same PERMISSIONS on the API
    // (AdminGate), but the Admin area is the administrator's home turf, deliberately kept off
    // every ordinary role's sidebar — exactly as the old dashboard panels were.
    public static readonly Role[] AdministratorOnly = Array.Empty<Role>();

    // Directors only: the company's most sensitive figures (the bank position), the settlement
    // mapping, and the AI doctrine store.
    public static readonly Role[] DirectorRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector
    };

    // The project's paperwork — the record chain (RFIs, variations), inventory, the project's
    // correspondence: the project roles less the H&S officer, whose project is the site.
    public static readonly Role[] ProjectPaperworkRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor,
        Role.SiteManager,
        Role.OfficeComplianceCoordinator,
        Role.OfficeAdmin,
        Role.SalesMarketing
    };

    // The RFI and Variation Orders rows: the paperwork roles plus the architect, who answers RFIs
    // and instructs variations on their own projects (the pages' InternalAndArchitect gate).
    public static readonly Role[] RequestRoles =
        ProjectPaperworkRoles.Append(Role.Architect).ToArray();

    // The Documents row: every project role, the foreman (who builds from them) and the
    // architect (who issues them) — the pages' DrawingReaders gate, less the subcontractor,
    // whose drawings reach them through their portal.
    public static readonly Role[] DocumentRoles =
        ProjectRoles.Append(Role.Foreman).Append(Role.Architect).ToArray();

    // What is happening on site — programme, calendar, progress, site instructions, the office's
    // site notes: every project role and the foreman.
    public static readonly Role[] SiteRoles =
        ProjectRoles.Append(Role.Foreman).ToArray();

    // Who changes a project's settings: the directors and the project manager.
    public static readonly Role[] ProjectSettingsRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager
    };

    // Who runs a tender: the commercial team and the office roles that assemble the packages.
    public static readonly Role[] BidPackageRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor,
        Role.OfficeComplianceCoordinator,
        Role.OfficeAdmin,
        Role.SalesMarketing
    };

    // Who places and reads work orders: the tender roles plus the site manager, who calls the
    // trades off against them.
    public static readonly Role[] WorkOrderRoles =
        BidPackageRoles.Append(Role.SiteManager).ToArray();

    // The company registers (insurances, subscriptions, vans): mirrors the API's
    // RegisterRoleSets.ManageRegisters.
    public static readonly Role[] RegisterRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.OfficeComplianceCoordinator,
        Role.OfficeAdmin,
        Role.SalesMarketing
    };

    // Staff sign-off forms: every member of staff, the site floor included.
    public static readonly Role[] PolicyRoles =
        AllInternalRoles.Append(Role.SiteOperative).ToArray();

    // Who reads the picked site's labour: the people who manage workers, plus the QS who costs
    // the hours and the site manager who signs them off.
    public static readonly Role[] SiteLabourRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor,
        Role.SiteManager
    };

    // Mirrors the API's XeroReportRoles.TransactionReaders.
    public static readonly Role[] XeroTransactionRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.QuantitySurveyor
    };

    // Mirrors the API's XeroReportRoles.AgedReportReaders: the commercial team and Accounts.
    public static readonly Role[] AgedReportRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor,
        Role.Accounts
    };

    // Who works the Sales folder: mirrors the API's SalesRoles.SalesTeam.
    public static readonly Role[] SalesRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor,
        Role.SalesMarketing
    };

    // Who files documents out of the triage queue: mirrors the API's
    // DocumentControlRoles.AllowedToManage.
    public static readonly Role[] DocumentTriageRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor,
        Role.OfficeComplianceCoordinator,
        Role.OfficeAdmin,
        Role.SalesMarketing
    };

    // The Weekly Cashflow row: the directors plus Accounts (decision 2026-08-27) — the page is
    // the accountant's working tool (he moves the payment weeks). Mirrors the API's
    // WeeklyCashflowGates.WeeklyCashflowRoles — keep the two lists in step. The bank-balance line inside the page stays directors-only (it
    // reads the Xero cash summary, whose gate is untouched).
    public static readonly Role[] WeeklyCashflowRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.Accounts
    };

    // Who opens the Directory: mirrors the API's DirectoryRoles.AllowedToList less the foreman,
    // whose sidebar is the site (2026-09-22; was MD/FD/PM from 2026-07-22).
    public static readonly Role[] DirectoryRoles = ProjectRoles;

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
    public static readonly Role[] AssistantRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor
    };
}
