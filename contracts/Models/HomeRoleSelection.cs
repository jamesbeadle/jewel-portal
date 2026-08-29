namespace Jewel.JPMS.Models;

/// <summary>
/// Which of a user's directory-assigned roles is "their own" — the one the "Viewing as" switcher
/// defaults back to when DirectoryUser.RevertToOwnRole is on.
///
/// The directory does not record the order roles were assigned in (role rows carry random ids and
/// are replaced wholesale on every edit), so "first assigned role" is defined as: the first role
/// on the user's directory entry that is NOT Administrator, in the standard Role declaration
/// order. Administrator is skipped because for anyone who holds it alongside a real role (the
/// Finance Director being the motivating case) Administrator is the elevated hat, not who they
/// are. A user whose only directory role IS Administrator gets Administrator back.
///
/// Shared by the API (auth responses) and the client (Admin → Users row), so both always name the
/// same role. Takes the RAW directory roles — never the Admin-expanded effective list, which
/// contains every role and would make the answer meaningless.
/// </summary>
public static class HomeRoleSelection
{
    public static Role? From(IReadOnlyList<Role> directoryRoles)
    {
        if (directoryRoles.Count == 0) return null;
        foreach (var role in Enum.GetValues<Role>())
        {
            if (role == Role.Admin) continue;
            if (directoryRoles.Contains(role)) return role;
        }
        return directoryRoles.Contains(Role.Admin) ? Role.Admin : directoryRoles[0];
    }
}
