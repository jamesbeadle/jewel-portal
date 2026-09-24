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

    /// <summary>The external roles. Each is given only by an invite from the account it belongs
    /// to — the Clients, Architects or Directory page — which links the login to that client,
    /// practice or company; a login holding one without the link reaches nothing.</summary>
    public static readonly IReadOnlyList<Role> ScopedByALink =
        new[] { Role.Client, Role.Architect, Role.Subcontractor };

    /// <summary>The roles an administrator gives by hand — everything but the external roles.</summary>
    public static readonly IReadOnlyList<Role> AssignedByHand =
        All.Where(role => !ScopedByALink.Contains(role)).ToArray();

    /// <summary>Whether a login holding these directory roles is one of the business's own —
    /// any role an administrator gives by hand. Such a login is never also made an external
    /// party's: the two readings of one person would disagree about what they may see.</summary>
    public static bool IncludeStaff(IEnumerable<Role> directoryRoles) => directoryRoles.Any(AssignedByHand.Contains);
}
