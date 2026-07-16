using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// The ROLES a to-do can be assigned to (internal office/management roles —
// TodoRoles.AssignableAsTodoAssignee), in picker order. This is the picker feed for anyone who
// can manage to-dos. Items are assigned to a role rather than a person, so the pool is a fixed
// role list, not the directory: whoever holds the role sees the item, and staff changes never
// orphan an assignment.
public sealed class ListTodoAssignableRolesHandler
    : IQueryHandler<ListTodoAssignableRoles, IReadOnlyList<Role>>
{
    public Task<IReadOnlyList<Role>> HandleAsync(
        ListTodoAssignableRoles query, CancellationToken cancellationToken) =>
        Task.FromResult(TodoRoles.AssignableTodoRolesInPickerOrder);
}
