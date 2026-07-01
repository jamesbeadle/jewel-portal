using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class UploadDrawingRevisionAuthorisation
{
    // Administrator, Managing Director and Project Manager may upload drawings and change their status.
    private static readonly RoleSet RolesThatMayManageDrawings =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user) =>
        RolesThatMayManageDrawings.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, UploadDrawingRevision command) => Allows(user);
}
