using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Gates;

/// <summary>Shared role resolution: everyone gets their directory roles, and a directory Admin
/// role expands to EVERY role — administrators are administered in the directory like anyone
/// else (the old hard-coded JpmsAdministrators list is gone), but the Admin role keeps its
/// carries-every-role meaning that gates and comments across the app rely on. Finance Directors
/// get admin-equivalent permissions via AdminGate, not role expansion.
/// SignedInUserResolver resolves through these same helpers, so the two paths cannot drift.</summary>
public static class UserRoles
{
    /// <summary>The RAW directory-assigned roles, exactly as administered — no Admin expansion.
    /// This is the list HomeRoleSelection must be fed; the expanded list would contain every
    /// role and make "the user's own role" meaningless.</summary>
    public static async Task<IReadOnlyList<Role>> DirectoryRolesAsync(JpmsContext context, string email, CancellationToken cancellationToken) =>
        await context.DirectoryUserRoles
            .Where(row => row.DirectoryUserEmail == email)
            .Select(row => (Role)row.Role)
            .ToListAsync(cancellationToken);

    /// <summary>The effective role list gates run on: a directory Admin role expands to every
    /// role; everyone else keeps exactly what the directory assigns.</summary>
    public static IReadOnlyList<Role> Expand(IReadOnlyList<Role> directoryRoles)
    {
        if (directoryRoles.Contains(Role.Admin)) return Enum.GetValues<Role>();
        // Finance Directors keep their own identity: their role list stays exactly what the
        // directory assigns. Admin-equivalent permissions are granted where they matter via
        // AdminGate, not by rewriting the role list (which made the client treat FDs as
        // admins and land them on the admin dashboard).
        return directoryRoles;
    }

    public static async Task<IReadOnlyList<Role>> ForAsync(JpmsContext context, string email, CancellationToken cancellationToken) =>
        Expand(await DirectoryRolesAsync(context, email, cancellationToken));
}
