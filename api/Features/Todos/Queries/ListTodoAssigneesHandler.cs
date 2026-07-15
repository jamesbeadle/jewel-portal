using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// Directory users filtered to the roles a to-do can be assigned to (internal office/management
// staff — TodoRoles.AssignableAsTodoAssignee). Mirrors ListDirectoryUsersHandler's shape, but
// this is the picker feed for anyone who can manage to-dos, not the admin-only directory.
public sealed class ListTodoAssigneesHandler
    : IQueryHandler<ListTodoAssignees, IReadOnlyList<DirectoryUser>>
{
    private readonly JpmsContext context;

    public ListTodoAssigneesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<DirectoryUser>> HandleAsync(
        ListTodoAssignees query, CancellationToken cancellationToken)
    {
        var users = await context.DirectoryUsers.ToListAsync(cancellationToken);
        var roleRows = await context.DirectoryUserRoles.ToListAsync(cancellationToken);
        return users
            .Select(user => user.ToModel(RolesFor(user.Email, roleRows)))
            .Where(user => TodoRoles.AssignableAsTodoAssignee.IncludesAny(user.Roles))
            .OrderBy(user => user.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    private static IReadOnlyList<Role> RolesFor(string email, IReadOnlyList<DirectoryUserRoleEntity> roleRows) =>
        roleRows
            .Where(row => string.Equals(row.DirectoryUserEmail, email, StringComparison.OrdinalIgnoreCase))
            .Select(row => (Role)row.Role)
            .ToList()
            .AsReadOnly();
}
