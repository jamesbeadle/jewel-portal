using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Gates;

/// <summary>Shared role resolution: master admins get every role, everyone else gets their directory roles.
/// Finance Directors are also granted admin-equivalent access — holding the FinanceDirector role expands to
/// every role (kept in sync with SignedInUserResolver.ResolveRolesAsync).</summary>
public static class UserRoles
{
    public static async Task<IReadOnlyList<Role>> ForAsync(JpmsContext context, string email, CancellationToken cancellationToken)
    {
        if (JpmsAdministrators.Contains(email)) return Enum.GetValues<Role>();
        var roles = await context.DirectoryUserRoles
            .Where(row => row.DirectoryUserEmail == email)
            .Select(row => (Role)row.Role)
            .ToListAsync(cancellationToken);
        // Finance Directors are granted admin-equivalent access: holding the FinanceDirector
        // role expands to every role, so an FD passes every gate exactly as a master admin does.
        if (roles.Contains(Role.FinanceDirector)) return Enum.GetValues<Role>();
        return roles;
    }
}
