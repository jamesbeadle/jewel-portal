using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class PromoteRequestToRfiAuthorisation
{
    // Promoting a request to an RFI issues an official document to the architect, so it is limited to
    // Administrator, Managing Director and Project Manager.
    private static readonly RoleSet RolesThatMayPromote =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, PromoteRequestToRfi command) =>
        RolesThatMayPromote.IncludesAny(user.Roles);
}
