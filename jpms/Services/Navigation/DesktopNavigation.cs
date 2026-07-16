using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services.Navigation;

public static class DesktopNavigation
{
    public static IReadOnlyList<NavigationItem> ItemsVisibleTo(Role role)
    {
        if (role == Role.Admin) return Entries.Select(entry => entry.Item).ToList();
        return Entries.Where(entry => entry.IsVisibleTo(role)).Select(entry => entry.Item).ToList();
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

    // The former single-page entries (Xero, Allocation, Cost codes, Rates, To-dos, RFIs, Triage,
    // Workers) are folded into three workspace sections mirroring the project view's sections.
    // Each section entry lands on its first tab and WorkspaceSectionNav renders the section's tab
    // row on every page in it; entry visibility is the union of the folded pages' role lists
    // (WorkspaceSections keeps the per-page gates, so the union widens nothing).
    private static readonly IReadOnlyList<DesktopNavigationEntry> Entries = new DesktopNavigationEntry[]
    {
        Entry("Dashboard",      "/dashboard",      AllRoles),
        // Site operatives' own timesheet page — sign in/out and end-of-day hours.
        Entry("My day",         "/my-day",         Role.SiteOperative, Role.Foreman, Role.SiteManager),
        Entry("Projects",       "/projects",       Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.QuantitySurveyor, Role.SiteManager, Role.HealthSafetyOfficer, Role.OfficeComplianceCoordinator),
        // Financials — the company-wide viewer plus Allocation, Xero, Cost codes and Rates.
        Entry(new NavigationItem("Financials", "/finance", new[] { "/cost-codes", "/rate-library" }),
              Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.QuantitySurveyor),
        // Project management — To-dos, RFIs and mailbox Triage.
        Entry(new NavigationItem("Project Management", "/todos", new[] { "/rfis", "/requests/triage" }),
              Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.QuantitySurveyor, Role.SiteManager, Role.HealthSafetyOfficer, Role.OfficeComplianceCoordinator),
        // Operations — the labour registry (Workers).
        Entry(new NavigationItem("Operations", "/labour/workers"),
              Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager),
        Entry("Directory",      "/directory",      Role.ManagingDirector),
        Entry("Clients",        "/clients",        Role.ManagingDirector, Role.ProjectManager),
        Entry("Architects",     "/architects",     Role.ManagingDirector, Role.ProjectManager),
        // The agent queue mirrors the API's AgentRoles.AllowedToOperateAgents gate (admins see
        // everything via the early return above).
        Entry("Agents",         "/agents",         Role.ManagingDirector, Role.ProjectManager, Role.QuantitySurveyor, Role.SiteManager)
    };

    private static DesktopNavigationEntry Entry(string label, string href, params Role[] visibleTo) =>
        new(new NavigationItem(label, href), visibleTo);

    private static DesktopNavigationEntry Entry(NavigationItem item, params Role[] visibleTo) =>
        new(item, visibleTo);
}
