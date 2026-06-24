using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Gates;

/// <summary>Shared role resolution: master admins get every role, everyone else gets their directory roles.</summary>
public static class UserRoles
{
    public static async Task<IReadOnlyList<Role>> ForAsync(JpmsContext context, string email, CancellationToken cancellationToken)
    {
        if (JpmsAdministrators.Contains(email)) return Enum.GetValues<Role>();
        return await context.DirectoryUserRoles
            .Where(row => row.DirectoryUserEmail == email)
            .Select(row => (Role)row.Role)
            .ToListAsync(cancellationToken);
    }
}
