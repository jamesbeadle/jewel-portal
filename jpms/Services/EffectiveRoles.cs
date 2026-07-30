using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public static class EffectiveRoles
{
    private static readonly IReadOnlyList<Role> AllRoles = Enum.GetValues<Role>();

    public static IReadOnlyList<Role> For(string email, DirectoryUser? directoryEntry)
    {
        // Mirrors the server's UserRoles/SignedInUserResolver: roles come from the directory,
        // and the Admin role expands to every role.
        if (directoryEntry is null) return Array.Empty<Role>();
        if (directoryEntry.Roles.Contains(Role.Admin)) return AllRoles;
        return directoryEntry.Roles;
    }
}
