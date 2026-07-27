namespace Jewel.JPMS.Models;

public enum Role
{
    Admin,
    ManagingDirector,
    FinanceDirector,
    ProjectManager,
    QuantitySurveyor,
    SiteManager,
    HealthSafetyOfficer,
    OfficeComplianceCoordinator,
    Architect,
    Client,
    Subcontractor,
    Foreman,

    // Day-rate site operatives logging their own time on the My Day page. Same account /
    // password / session model as every other user (docs/Labour-Time-Tracking-Scope.md).
    SiteOperative,

    // Accounts / bookkeeping. An internal back-office role that sits BELOW the Finance Director:
    // it exists so accounts-based to-dos have an assignee of their own instead of everything
    // landing on the FD. It carries none of the FD's money-facing reach — deliberately absent
    // from JpmsRoleSets.CommercialTeam (cashflow, Xero, ledger detail) and from every director
    // gate (DesktopNavigation.DirectorRoles, FinanceRoles, TodoRoles.AllowedToSeeAllTodos).
    //
    // NOTE: roles persist as their integer value (DirectoryUserRoles.Role, TodoItems.AssigneeRole),
    // so new members are APPENDED here and never inserted mid-list.
    Accounts
}
