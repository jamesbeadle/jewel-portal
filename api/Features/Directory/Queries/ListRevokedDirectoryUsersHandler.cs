using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Queries;

public sealed class ListRevokedDirectoryUsersHandler
    : IQueryHandler<ListRevokedDirectoryUsers, IReadOnlyList<RevokedDirectoryUser>>
{
    private readonly JpmsContext context;

    public ListRevokedDirectoryUsersHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<RevokedDirectoryUser>> HandleAsync(
        ListRevokedDirectoryUsers query, CancellationToken cancellationToken)
    {
        var users = await context.DirectoryUsers.AsNoTracking()
            .Where(user => user.RevokedAt != null)
            .ToListAsync(cancellationToken);
        var roleRows = await context.DirectoryUserRoles.AsNoTracking().ToListAsync(cancellationToken);
        // Most recently revoked first — the row an administrator is looking for is almost always
        // the one they just revoked.
        return users
            .OrderByDescending(user => user.RevokedAt)
            .Select(user => user.ToRevokedModel(RolesFor(user.Email, roleRows)))
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
