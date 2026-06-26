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
        Entry("Leads",          "/leads",          Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.QuantitySurveyor),
        Entry("Projects",       "/projects",       Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.QuantitySurveyor, Role.SiteManager, Role.HealthSafetyOfficer, Role.OfficeComplianceCoordinator),
        Entry("Subcontractors", "/subcontractors", Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.HealthSafetyOfficer, Role.OfficeComplianceCoordinator),
        Entry("Work orders",    "/work-orders",    Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.QuantitySurveyor, Role.OfficeComplianceCoordinator),
        Entry("Triage",         "/requests/triage", Role.ManagingDirector, Role.ProjectManager, Role.QuantitySurveyor, Role.SiteManager, Role.HealthSafetyOfficer, Role.OfficeComplianceCoordinator, Role.Foreman),
        Entry("H&S",            "/hs",             Role.ManagingDirector, Role.HealthSafetyOfficer),
        Entry("Cashflow",       "/cashflow",       Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager),
        Entry("Portfolio",      "/portfolio",      Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.QuantitySurveyor, Role.HealthSafetyOfficer),
        Entry("Reports",        "/reports",        Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.QuantitySurveyor),
        Entry("Rates",          "/rate-library",   Role.ManagingDirector, Role.ProjectManager, Role.QuantitySurveyor),
        Entry("Site app",       "/site/today",     Role.SiteManager, Role.Foreman)
    };

    private static DesktopNavigationEntry Entry(string label, string href, params Role[] visibleTo) =>
        new(new NavigationItem(label, href), visibleTo);
}
