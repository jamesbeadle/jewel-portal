using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// The pool of PEOPLE a to-do can be pinned to, one row per (role, holder) pair: every directory
// user holding one of the to-do-assignable roles (TodoRoles.AssignableAsTodoAssignee in the api),
// listed under each assignable role they hold. A person is only ever pinned WITH a role — see
// TodoAssignee — so the picker feed is grouped by role, in the same order ListTodoAssignableRoles
// presents them, people A–Z by display name within each. Feeds the assignee pickers alongside
// ListTodoAssignableRoles (triage's to-do form, the add modals, the detail modal's reassign).
public sealed record ListTodoAssignablePeople : IQuery<IReadOnlyList<TodoAssignablePerson>>;
