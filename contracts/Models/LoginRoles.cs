namespace Jewel.JPMS.Models;

/// <summary>
/// The roles a PERSON can hold — every Role but the to-do desk Role.Miscellaneous, which names
/// an owner-less to-do and is never assigned to a login. The user pickers (invite, role
/// assignment, the approved-user row) and the administrator's expansion read this, not the enum.
/// </summary>
public static class LoginRoles
{
    public static readonly IReadOnlyList<Role> All =
        Enum.GetValues<Role>().Where(role => role != Role.Miscellaneous).ToArray();
}
