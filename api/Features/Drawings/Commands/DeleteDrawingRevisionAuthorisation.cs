using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class DeleteDrawingRevisionAuthorisation
{
    // Administrator, Managing Director and Project Manager may delete drawing revisions.
    private static readonly RoleSet RolesThatMayManageDrawings =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, DeleteDrawingRevision command) =>
        RolesThatMayManageDrawings.IncludesAny(user.Roles);
}
