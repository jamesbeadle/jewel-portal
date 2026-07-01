using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class LinkRequestToClientAuthorisation
{
    private static readonly RoleSet RolesThatMayLinkClient =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, LinkRequestToClient command) =>
        RolesThatMayLinkClient.IncludesAny(user.Roles);
}
