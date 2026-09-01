using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Todos;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// The PEOPLE a to-do can be pinned to: every directory user holding one of the assignable roles
// (TodoRoles.AssignableAsTodoAssignee), one row per (role, holder) pair so the pickers can list
// each person under each assignable role they hold. Grouped in the same order the roles picker
// uses (AssignableTodoRolesInPickerOrder), people A–Z by display name within a role. A person is
// only ever pinned WITH a role — see TodoAssignee — which is why this is (role, person) pairs and
// not a flat person list.
public sealed class ListTodoAssignablePeopleHandler
    : IQueryHandler<ListTodoAssignablePeople, IReadOnlyList<TodoAssignablePerson>>
{
    private readonly JpmsContext context;
    public ListTodoAssignablePeopleHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<TodoAssignablePerson>> HandleAsync(
        ListTodoAssignablePeople query, CancellationToken cancellationToken)
    {
        var assignableValues = TodoRoles.AssignableTodoRolesInPickerOrder.Select(role => (int)role).ToList();
        var roleRows = await context.DirectoryUserRoles.AsNoTracking()
            .Where(row => assignableValues.Contains(row.Role))
            .ToListAsync(cancellationToken);
        // Revoked users keep their role rows (so a restore puts them back as they were), but a
        // person who cannot sign in must not be offered as a pin target.
        var users = await context.DirectoryUsers.AsNoTracking()
            .Where(user => user.RevokedAt == null)
            .ToListAsync(cancellationToken);
        var usersByEmail = users.ToDictionary(user => user.Email, StringComparer.OrdinalIgnoreCase);

        return TodoRoles.AssignableTodoRolesInPickerOrder
            .SelectMany(role => roleRows
                .Where(row => row.Role == (int)role)
                .Select(row => usersByEmail.TryGetValue(row.DirectoryUserEmail, out var user) ? user : null)
                .Where(user => user is not null)
                .Select(user => new TodoAssignablePerson(role, user!.Email, user.DisplayName))
                .OrderBy(person => person.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(person => person.Email, StringComparer.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }
}
