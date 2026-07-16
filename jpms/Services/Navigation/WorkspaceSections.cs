using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services.Navigation;

/// <summary>
/// The cross-project (workspace-level) mirror of the project view's three sections. Each side-nav
/// section entry lands on its first tab and <c>WorkspaceSectionNav</c> renders the section's tab
/// row on every page folded into it. Per-tab role lists reproduce the visibility the pages had as
/// standalone side-nav entries — folding them into sections must not widen who can reach them.
/// Administrators see every tab (mirrored from DesktopNavigation's admin early-return).
/// </summary>
public sealed record WorkspaceTab(string Label, string Href, IReadOnlyList<Role> VisibleTo)
{
    public bool IsVisibleTo(Role role) => role == Role.Admin || VisibleTo.Contains(role);
}

public sealed record WorkspaceSectionInfo(string Label, IReadOnlyList<WorkspaceTab> Tabs)
{
    public bool Owns(string path) => BestMatch(path) is not null;

    /// <summary>
    /// The tab that owns a path — exact match or a sub-route of it. Longest href wins so
    /// /finance/xero activates the Xero tab, not the /finance Overview tab.
    /// </summary>
    public WorkspaceTab? BestMatch(string path) =>
        Tabs.Where(tab => path == tab.Href || path.StartsWith(tab.Href + "/", StringComparison.Ordinal))
            .OrderByDescending(tab => tab.Href.Length)
            .FirstOrDefault();

    public IReadOnlyList<WorkspaceTab> TabsVisibleTo(Role role) =>
        Tabs.Where(tab => tab.IsVisibleTo(role)).ToList();
}

public static class WorkspaceSections
{
    private static readonly Role[] AllInternalRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor,
        Role.SiteManager,
        Role.HealthSafetyOfficer,
        Role.OfficeComplianceCoordinator
    };

    public static readonly WorkspaceSectionInfo Financials = new(
        "Financials",
        new[]
        {
            // The company-wide financials viewer — totals across every project or one project at a time.
            new WorkspaceTab("Financials", "/finance",
                new[] { Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.QuantitySurveyor }),
            // Mirrors the API's allocation authorisation (XeroLedgerRoles.AllowedToAllocate).
            new WorkspaceTab("Allocation", "/finance/allocation",
                new[] { Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.QuantitySurveyor }),
            // Mirrors the API's Xero ledger authorisation (ListXeroTransactionsEndpoint).
            new WorkspaceTab("Xero", "/finance/xero",
                new[] { Role.ManagingDirector, Role.FinanceDirector, Role.QuantitySurveyor }),
            // Mirrors the API's cost-centre command authorisation.
            new WorkspaceTab("Cost codes", "/cost-codes",
                new[] { Role.ManagingDirector, Role.FinanceDirector, Role.QuantitySurveyor }),
            new WorkspaceTab("Rates", "/rate-library",
                new[] { Role.ManagingDirector, Role.ProjectManager, Role.QuantitySurveyor })
        });

    public static readonly WorkspaceSectionInfo ProjectManagement = new(
        "Project Management",
        new[]
        {
            new WorkspaceTab("To-dos", "/todos", AllInternalRoles),
            new WorkspaceTab("RFIs", "/rfis", AllInternalRoles),
            // Mailbox triage — mirrors the API's TriageRoles.AllowedToTriage gate.
            new WorkspaceTab("Triage", "/requests/triage",
                new[] { Role.ProjectManager, Role.FinanceDirector })
        });

    public static readonly WorkspaceSectionInfo Operations = new(
        "Operations",
        new[]
        {
            // Mirrors the API's labour registry authorisation (LabourRoleSets.ManageWorkers).
            new WorkspaceTab("Workers", "/labour/workers",
                new[] { Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager })
        });

    public static readonly IReadOnlyList<WorkspaceSectionInfo> All =
        new[] { Financials, ProjectManagement, Operations };

    public static WorkspaceSectionInfo? SectionOwning(string path) =>
        All.FirstOrDefault(section => section.Owns(path));
}
