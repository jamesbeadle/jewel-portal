using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class DeleteDrawingAuthorisation
{
    // Administrator, Managing Director and Project Manager may delete drawings.
    private static readonly RoleSet RolesThatMayManageDrawings =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, DeleteDrawing command) =>
        RolesThatMayManageDrawings.IncludesAny(user.Roles);
}
