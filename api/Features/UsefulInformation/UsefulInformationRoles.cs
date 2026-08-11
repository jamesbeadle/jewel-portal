using Jewel.JPMS.Api.Gates;

namespace Jewel.JPMS.Api.Features.UsefulInformation;

// Useful Information notes are internal reference material — door codes, key safe locations, site
// access notes — written by office administrators and read by anyone on staff. Every internal role
// may both read and edit (decision 2026-08-11): the notes exist to save the office a phone call,
// and whoever discovers the gate code changed should be able to fix the note on the spot.
// External roles (Architect, Client, Subcontractor) are deliberately outside both gates — this is
// exactly the kind of content that must never leak to a portal login. Administrators pass every
// gate (SignedInUserResolver grants them all roles).
internal static class UsefulInformationRoles
{
    public static readonly RoleSet AllowedToRead = JpmsRoleSets.AllInternal;

    // Same set as reading, kept as its own name so a future narrowing (e.g. admins-only editing)
    // is a one-line change here rather than a hunt through the endpoints.
    public static readonly RoleSet AllowedToManage = JpmsRoleSets.AllInternal;
}
