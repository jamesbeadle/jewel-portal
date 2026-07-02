using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

// Editing the official document's body is the same calibre of action as editing the request's
// details, so it carries the same gate.
public sealed class UpdateRequestFormAuthorisation
{
    private static readonly RoleSet RolesThatMayUpdateForm =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Architect);

    public bool Allows(SignedInUser user, UpdateRequestForm command) => RolesThatMayUpdateForm.IncludesAny(user.Roles);
}
