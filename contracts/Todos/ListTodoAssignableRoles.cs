using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// The pool of ROLES a to-do item can be assigned to: internal office/management roles only.
// External and site-floor roles (Architect, Client, Subcontractor, Foreman, SiteOperative) are
// excluded server-side — see TodoRoles.AssignableAsTodoAssignee in the api. Feeds the assignee
// role pickers (triage's to-do form, the project To-do tab's add modal, the To-dos browser).
public sealed record ListTodoAssignableRoles : IQuery<IReadOnlyList<Role>>;
