using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Gates;

/// <summary>Shared role resolution: master admins get every role, everyone else gets their directory
/// roles. Finance Directors get admin-equivalent permissions via AdminGate, not role expansion
/// (kept in sync with SignedInUserResolver.ResolveRolesAsync).</summary>
public static class UserRoles
{
    public static async Task<IReadOnlyList<Role>> ForAsync(JpmsContext context, string email, CancellationToken cancellationToken)
    {
        if (JpmsAdministrators.Contains(email)) return Enum.GetValues<Role>();
        var roles = await context.DirectoryUserRoles
            .Where(row => row.DirectoryUserEmail == email)
            .Select(row => (Role)row.Role)
            .ToListAsync(cancellationToken);
        // Finance Directors keep their own identity: their role list stays exactly what the
        // directory assigns. Admin-equivalent permissions are granted where they matter via
        // AdminGate, not by rewriting the role list (which made the client treat FDs as
        // admins and land them on the admin dashboard). Keep in sync with the other resolver.
        return roles;
    }
}
