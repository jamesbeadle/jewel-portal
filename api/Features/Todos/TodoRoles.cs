using Jewel.JPMS.Api.Gates;

namespace Jewel.JPMS.Api.Features.Todos;

// Project to-dos are a back-office project-management surface. Directors (managing and finance),
// project managers, site managers and accounts may manage them; administrators pass via Role.Admin
// (they are granted every role server-side anyway, mirroring TriageRoles' belt-and-braces inclusion).
internal static class TodoRoles
{
    public static readonly RoleSet AllowedToManageTodos =
        RoleSet.Of(
            Role.Admin,
            JpmsRoles.Director,
            JpmsRoles.FinanceDirector,
            JpmsRoles.ProjectManager,
            JpmsRoles.SiteManager,
            // Accounts raises and assigns its own accounts-based items. Managing to-dos is NOT
            // seeing every to-do: AllowedToSeeAllTodos below stays MD + admin, so Accounts still
            // reads only the items assigned to a role it holds.
            JpmsRoles.Accounts);

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
    //
    // The ADMINISTRATOR super-role (Role.Admin) is deliberately NOT here (decision 2026-08-07):
    // it is a system role that carries every other role, not a desk work lands on. The internal
    // lower-level OfficeAdmin role is the assignable "admin" — existing Administrator-assigned
    // items were remapped by scripts/2026-08-07-todoitems-admin-to-office-admin.sql.
    public static readonly IReadOnlyList<Role> AssignableTodoRolesInPickerOrder = new[]
    {
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        // Accounts sits directly under the FD in the picker: it is where the accounts-based
        // items go that used to have nowhere to land but the FD.
        JpmsRoles.Accounts,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.SiteManager,
        JpmsRoles.HealthAndSafetyLead,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin
    };

    // The same pool as a set, for gate checks ("is this AssigneeRole value allowed?").
    public static readonly RoleSet AssignableAsTodoAssignee =
        RoleSet.Of(AssignableTodoRolesInPickerOrder.ToArray());
}
