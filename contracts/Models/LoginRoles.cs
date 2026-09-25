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

    /// <summary>The external roles whose login is linked to an account it belongs to — given only
    /// by an invite from the Clients page or the Directory, which links the login to that client or
    /// company; a login holding one without the link reaches nothing. The architect is not among
    /// them: an architect login is given its projects in Admin → Users (2026-09-25).</summary>
    public static readonly IReadOnlyList<Role> ScopedByALink =
        new[] { Role.Client, Role.Subcontractor };

    /// <summary>The roles an administrator gives by hand — everything but the linked external roles.</summary>
    public static readonly IReadOnlyList<Role> AssignedByHand =
        All.Where(role => !ScopedByALink.Contains(role)).ToArray();

    /// <summary>Whether a login holding these directory roles is one of the business's own — an
    /// administrator or any internal role. Such a login is never also made an external party's:
    /// the two readings of one person would disagree about what they may see.</summary>
    public static bool IncludeStaff(IEnumerable<Role> directoryRoles) =>
        directoryRoles.Any(role => role == Role.Admin || JpmsRoleSets.AllInternal.Includes(role));
}
