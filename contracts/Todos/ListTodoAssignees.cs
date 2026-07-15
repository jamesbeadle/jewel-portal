using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// The pool of users a to-do item can be assigned to: internal office/management staff only.
// External and site-floor roles (Architect, Client, Subcontractor, Foreman, SiteOperative) are
// excluded server-side — see TodoRoles.AssignableAsTodoAssignee in the api.
public sealed record ListTodoAssignees : IQuery<IReadOnlyList<DirectoryUser>>;
