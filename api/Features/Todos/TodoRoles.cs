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
    // items — the ones assigned to a role they hold — through ListMyTodoItems.
    public static readonly RoleSet AllowedToSeeAllTodos =
        RoleSet.Of(
            Role.Admin,
            JpmsRoles.Director);

    // The ROLES a to-do can be assigned to, in the order the pickers present them: internal
    // office/management roles. Deliberately narrower than JpmsRoleSets.AllInternal — besides the
    // external roles (Architect, Client, Subcontractor) it also excludes Foreman and SiteOperative,
    // who work the site rather than the to-do list. Items are assigned to a role, not a person, so
    // they survive staff changes; ListTodoAssignableRoles serves this list to the pickers.
    public static readonly IReadOnlyList<Role> AssignableTodoRolesInPickerOrder = new[]
    {
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.SiteManager,
        JpmsRoles.HealthAndSafetyLead,
        JpmsRoles.OfficeComplianceCoordinator,
        Role.Admin
    };

    // The same pool as a set, for gate checks ("is this AssigneeRole value allowed?").
    public static readonly RoleSet AssignableAsTodoAssignee =
        RoleSet.Of(AssignableTodoRolesInPickerOrder.ToArray());
}
