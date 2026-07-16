using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Todos;

// Project to-dos are a back-office project-management surface. Directors, project managers and site
// managers may manage them; administrators pass via Role.Admin (they are granted every role
// server-side anyway, mirroring TriageRoles' belt-and-braces inclusion).
internal static class TodoRoles
{
    public static readonly RoleSet AllowedToManageTodos =
        RoleSet.Of(
            Role.Admin,
            JpmsRoles.Director,
            JpmsRoles.ProjectManager,
            JpmsRoles.SiteManager);

    // Who sees EVERY to-do item in the To-dos browser (and may add/manage general, no-project
    // items there): the managing director and administrators only. Everyone else reads their own
    // assigned items through ListMyTodoItems.
    public static readonly RoleSet AllowedToSeeAllTodos =
        RoleSet.Of(
            Role.Admin,
            JpmsRoles.Director);

    // Who a to-do can be *assigned to*: internal office/management staff. Deliberately narrower
    // than JpmsRoleSets.AllInternal — besides the external roles (Architect, Client,
    // Subcontractor) it also excludes Foreman and SiteOperative, who work the site rather than
    // the to-do list. Backs the assignee picker's ListTodoAssignees query.
    public static readonly RoleSet AssignableAsTodoAssignee =
        RoleSet.Of(
            Role.Admin,
            JpmsRoles.Director,
            JpmsRoles.FinanceDirector,
            JpmsRoles.ProjectManager,
            JpmsRoles.Estimator,
            JpmsRoles.SiteManager,
            JpmsRoles.HealthAndSafetyLead,
            JpmsRoles.OfficeComplianceCoordinator);
}
