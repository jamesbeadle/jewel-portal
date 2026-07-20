using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services.Navigation;

/// <summary>
/// The sidebar catalog — the app's single navigation plane. Two scopes, made visually explicit:
/// the PROJECT WORKSPACE (everything under the project picker targets the picked project, via
/// {project} templates resolved against CurrentProjectService) and the COMPANY area below the
/// divider (cross-project tools). No page appears in both. There is deliberately no Projects
/// list entry — the portfolio (/projects, with New project) is reachable from the picker's
/// footer and the project breadcrumb. Role gates reproduce what each page had before; grouping
/// widens nothing, and administrators see everything.
/// </summary>
public static class DesktopNavigation
{
    /// <summary>A headed block of project pages (heading is an eyebrow, not a link).</summary>
    public sealed record SidebarBlock(string Heading, string IconKey, IReadOnlyList<NavigationItem> Items);

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

    // The internal office/management roles that can open projects.
    private static readonly Role[] ProjectRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor,
        Role.SiteManager,
        Role.HealthSafetyOfficer,
        Role.OfficeComplianceCoordinator
    };

    private static readonly Role[] FinanceRoles =
    {
        Role.ManagingDirector,
        Role.FinanceDirector,
        Role.ProjectManager,
        Role.QuantitySurveyor
    };

    public static readonly NavigationItem Home = new("Home", "/dashboard");

    public static readonly NavigationItem ProjectOverview = new("Overview", "/projects/{project}");

    public static readonly NavigationItem ProjectSettings = new("Project settings", "/projects/{project}/settings");

    public static bool CanSee(Role role, IReadOnlyList<Role> visibleTo) =>
        role == Role.Admin || visibleTo.Contains(role);

    public static bool CanSeeProjects(Role role) => CanSee(role, ProjectRoles);

    /// <summary>The project workspace's headed blocks, role-filtered. Built from ProjectSections
    /// so the sidebar and the landing-page cards can never drift apart.</summary>
    public static IReadOnlyList<SidebarBlock> ProjectBlocksFor(Role role)
    {
        var blocks = new List<SidebarBlock>();
        foreach (var section in ProjectSections.All)
        {
            var visibleTo = section.Section == ProjectSection.Financials ? FinanceRoles : ProjectRoles;
            if (!CanSee(role, visibleTo)) continue;
            blocks.Add(new SidebarBlock(
                section.Label,
                section.IconKey,
                section.Tabs.Select(tab => new NavigationItem(tab.Label, $"/projects/{{project}}/{tab.Slug}")).ToList()));
        }
        return blocks;
    }

    private static readonly (NavigationItem Item, Role[] VisibleTo)[] CompanyEntries =
    {
        // The mailbox intake queue — daily work for those who triage; mirrors the API's TriageRoles gate.
        (new NavigationItem("Triage", "/requests/triage"),
            new[] { Role.ProjectManager, Role.FinanceDirector }),
        // One row per active project plus the total. Exact-only: /finance/* belongs to Xero.
        (new NavigationItem("Financial Summary", "/finance", ExactMatch: true), FinanceRoles),
        // Allocation + Transactions as tabs of one page — Allocation leads (the working screen).
        (new NavigationItem("Xero", "/finance/allocation", new[] { "/finance/xero" }), FinanceRoles),
        (new NavigationItem("Cost codes & Rates", "/cost-codes", new[] { "/rate-library" }), FinanceRoles),
        // Mirrors the API's labour registry authorisation (LabourRoleSets.ManageWorkers).
        (new NavigationItem("Workers", "/labour/workers"),
            new[] { Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager }),
        (new NavigationItem("Directory", "/directory"), new[] { Role.ManagingDirector }),
        (new NavigationItem("Clients", "/clients"), new[] { Role.ManagingDirector, Role.ProjectManager }),
        (new NavigationItem("Architects", "/architects"), new[] { Role.ManagingDirector, Role.ProjectManager })
        // Agents and the flat To-dos/RFIs pages are retired from the nav; their routes remain.
    };

    public static IReadOnlyList<NavigationItem> CompanyItemsFor(Role role) =>
        CompanyEntries.Where(entry => CanSee(role, entry.VisibleTo)).Select(entry => entry.Item).ToList();

    /// <summary>Every navigable item in sidebar order — for flat consumers like the page-heading
    /// matcher. Company items come after project items, so the more specific project routes win.</summary>
    public static IReadOnlyList<NavigationItem> ItemsVisibleTo(Role role)
    {
        var items = new List<NavigationItem> { Home };
        if (CanSeeProjects(role))
        {
            items.Add(ProjectOverview);
            items.AddRange(ProjectBlocksFor(role).SelectMany(block => block.Items));
            items.Add(ProjectSettings);
        }
        items.AddRange(CompanyItemsFor(role));
        return items;
    }
}
