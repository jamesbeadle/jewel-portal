using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Gates;

/// <summary>
/// The single check for admin-only endpoints (user directory, invites, access requests).
/// Administrators hold Role.Admin as an ordinary directory role (role resolution expands it
/// to every role); Finance Directors are granted the same PERMISSIONS here without being
/// linked to the Admin identity — their role list stays exactly what the directory assigns,
/// so they land on the FD dashboard, not the admin one.
/// </summary>
public static class AdminGate
{
    public static bool Allows(SignedInUser user) =>
        user.Roles.Contains(Role.Admin) || user.Roles.Contains(Role.FinanceDirector);
}
