using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Parties;

// A party's contact book decides who receives Jewel's outbound request correspondence, so managing
// it is limited to the same back-office set that manages client accounts and architect practices.
public sealed class PartyContactAuthorisation
{
    private static readonly RoleSet RolesThatMayManagePartyContacts =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user) => RolesThatMayManagePartyContacts.IncludesAny(user.Roles);
}
