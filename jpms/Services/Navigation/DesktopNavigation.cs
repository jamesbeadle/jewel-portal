using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services.Navigation;

/// <summary>
/// The side nav: plain entries plus three accordion groups (Financials, Project Management,
/// Operations). The nav is the primary navigation — the project view keeps only its per-section
/// tab row, and section switching happens here. Project-scoped entries ({project} templates)
/// follow the last-viewed project via CurrentProjectService. A group is visible when any of its
/// children is; each child keeps the role gate its page had as a standalone entry, so grouping
/// widens nothing.
/// </summary>
public static class DesktopNavigation
{
    public static IReadOnlyList<NavigationNode> NodesVisibleTo(Role role)
    {
        var nodes = new List<NavigationNode>();
        foreach (var entry in Entries)
        {
            if (entry.Children is { Count: > 0 } children)
            {
                var visibleChildren = children
                    .Where(child => role == Role.Admin || child.IsVisibleTo(role))
                    .Select(child => child.Item)
                    .ToList();
                if (visibleChildren.Count > 0)
                    nodes.Add(new NavigationNode(entry.Item, visibleChildren));
            }
            else if (role == Role.Admin || entry.IsVisibleTo(role))
            {
                nodes.Add(new NavigationNode(entry.Item, Array.Empty<NavigationItem>()));
            }
        }
        return nodes;
    }

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

    // The internal office/management roles that can open projects — mirrors the old Projects
    // entry's list; project-scoped children reuse it.
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

    private static readonly IReadOnlyList<DesktopNavigationEntry> Entries = new[]
    {
        // My day lives on the Dashboard now (RoleHome renders the worker's day for site-floor
        // roles), so Dashboard is the whole top level for those roles.
        Entry(new NavigationItem("Dashboard", "/dashboard"), AllRoles),

        // ShallowMatch: lit on the project list and a project's landing page only — the project
        // tabs belong to the grouped entries below.
        Entry(new NavigationItem("Projects", "/projects", ShallowMatch: true), ProjectRoles),

        Group(new NavigationItem("Financials", "#financials"),
            // The project's money pages (Financials, Cashflow, Valuation Report, Setup tabs) plus
            // the All-projects overview at /finance — exact-only so /finance/xero stays Xero's.
            Entry(new NavigationItem("Project financials", "/projects/{project}/financials",
                    new[] { "/projects/{project}/cashflow", "/projects/{project}/valuation", "/projects/{project}/financials-setup", "/finance$" }),
                Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.QuantitySurveyor),
            // Transactions + Allocation as tabs of one page.
            Entry(new NavigationItem("Xero", "/finance/xero", new[] { "/finance/allocation" }),
                Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.QuantitySurveyor),
            Entry(new NavigationItem("Setup", "/cost-codes", new[] { "/rate-library" }),
                Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.QuantitySurveyor)),

        Group(new NavigationItem("Project Management", "#project-management"),
            Entry(new NavigationItem("To-do", "/projects/{project}/todos"), ProjectRoles),
            Entry(new NavigationItem("Requests", "/projects/{project}/requests"), ProjectRoles),
            Entry(new NavigationItem("Drawings", "/projects/{project}/drawings"), ProjectRoles),
            Entry(new NavigationItem("Programme", "/projects/{project}/programme"), ProjectRoles),
            Entry(new NavigationItem("Progress", "/projects/{project}/progress"), ProjectRoles),
            Entry(new NavigationItem("Communications", "/projects/{project}/communications"), ProjectRoles),
            Entry(new NavigationItem("Setup", "/projects/{project}/setup"), ProjectRoles),
            // The cross-project mailbox triage queue — mirrors the API's TriageRoles gate.
            Entry(new NavigationItem("Triage", "/requests/triage"), Role.ProjectManager, Role.FinanceDirector)),

        Group(new NavigationItem("Operations", "#operations"),
            // The project's delivery pages (Labour, Bid Package Invites, Work Orders, WO
            // Allocation, Setup tabs) — one entry; the tab row covers the rest.
            Entry(new NavigationItem("Project operations", "/projects/{project}/labour",
                    new[] { "/projects/{project}/bid-package-invites", "/projects/{project}/work-orders", "/projects/{project}/work-order-allocation", "/projects/{project}/operations-setup" }),
                ProjectRoles),
            // Mirrors the API's labour registry authorisation (LabourRoleSets.ManageWorkers).
            Entry(new NavigationItem("Workers", "/labour/workers"),
                Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager),
            Entry(new NavigationItem("Directory", "/directory"), Role.ManagingDirector),
            Entry(new NavigationItem("Clients", "/clients"), Role.ManagingDirector, Role.ProjectManager),
            Entry(new NavigationItem("Architects", "/architects"), Role.ManagingDirector, Role.ProjectManager))

        // Agents is retired from the nav (nothing to manage day-to-day); /agents stays routable.
    };

    private static DesktopNavigationEntry Entry(NavigationItem item, params Role[] visibleTo) =>
        new(item, visibleTo);

    private static DesktopNavigationEntry Group(NavigationItem item, params DesktopNavigationEntry[] children) =>
        new(item, Array.Empty<Role>(), children);
}
