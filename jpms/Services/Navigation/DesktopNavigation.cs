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

    private static readonly IReadOnlyList<DesktopNavigationEntry> Entries = new DesktopNavigationEntry[]
    {
        Entry("Dashboard",      "/dashboard",      AllRoles),
        Entry("Projects",       "/projects",       Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.QuantitySurveyor, Role.SiteManager, Role.HealthSafetyOfficer, Role.OfficeComplianceCoordinator),
        Entry("Directory",      "/directory",      Role.ManagingDirector),
        Entry("Clients",        "/clients",        Role.ManagingDirector, Role.ProjectManager),
        Entry("Architects",     "/architects",     Role.ManagingDirector, Role.ProjectManager),
        // Triage is restricted to administrators (who see everything via the early return above)
        // and project managers for now. A dedicated triage-visibility role can be added later.
        Entry("Triage",         "/requests/triage", Role.ProjectManager),
        // The agent queue mirrors the API's AgentRoles.AllowedToOperateAgents gate (admins see
        // everything via the early return above).
        Entry("Agents",         "/agents",          Role.ManagingDirector, Role.ProjectManager, Role.QuantitySurveyor, Role.SiteManager),
        Entry("Rates",          "/rate-library",   Role.ManagingDirector, Role.ProjectManager, Role.QuantitySurveyor)
    };

    private static DesktopNavigationEntry Entry(string label, string href, params Role[] visibleTo) =>
        new(new NavigationItem(label, href), visibleTo);
}
