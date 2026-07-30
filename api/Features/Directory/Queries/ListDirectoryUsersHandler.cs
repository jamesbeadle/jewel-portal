using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Directory.Queries;

public sealed class ListDirectoryUsersHandler
    : IQueryHandler<ListDirectoryUsers, IReadOnlyList<DirectoryUser>>
{
    private readonly JpmsContext context;

    public ListDirectoryUsersHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<DirectoryUser>> HandleAsync(
        ListDirectoryUsers query, CancellationToken cancellationToken)
    {
        // Revoked users are not listed here — they have their own read (ListRevokedDirectoryUsers)
        // behind the admin gate, so "the users" always means people who can actually sign in.
        var users = await context.DirectoryUsers.AsNoTracking()
            .Where(user => user.RevokedAt == null)
            .ToListAsync(cancellationToken);
        var roleRows = await context.DirectoryUserRoles.AsNoTracking().ToListAsync(cancellationToken);
        return users
            .Select(user => user.ToModel(RolesFor(user.Email, roleRows)))
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
