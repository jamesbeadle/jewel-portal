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
    Accounts,

    // General office administration — the internal lower-level "Office Admin" role, added
    // (2026-08-07) so day-to-day office to-dos have a proper assignee. NOT the Administrator
    // super-role (Role.Admin) above, which carries every role and is deliberately no longer
    // offered by the to-do assignment pickers (TodoRoles.AssignableTodoRolesInPickerOrder).
    // Access-wise it mirrors OfficeComplianceCoordinator: same project pages, same
    // subcontractor/procurement/drawing gates — kept side by side wherever that role appears.
    OfficeAdmin,

    // Sales & Marketing (2026-09-03) — the desk an inbound tender enquiry lands on now that the
    // Tender Enquiries register is retired (James: "we will just do this through todo"): an
    // architect's invitation becomes a to-do assigned to this role, and the assignee sees the
    // email on the to-do. Access-wise it mirrors OfficeAdmin (James's choice), so it is added
    // beside OfficeAdmin in every gate that role appears in. Persists as int 15 — appended, never
    // inserted mid-list.
    SalesMarketing
}
