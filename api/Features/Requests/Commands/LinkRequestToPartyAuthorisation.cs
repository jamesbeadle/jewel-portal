using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class LinkRequestToPartyAuthorisation
{
    private static readonly RoleSet RolesThatMayLinkParty =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, LinkRequestToParty command) =>
        RolesThatMayLinkParty.IncludesAny(user.Roles);
}
