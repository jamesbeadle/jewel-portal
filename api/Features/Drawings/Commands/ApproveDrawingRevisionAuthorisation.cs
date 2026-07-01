using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class ApproveDrawingRevisionAuthorisation
{
    // Administrator, Managing Director and Project Manager may change drawing statuses.
    private static readonly RoleSet RolesThatMayManageDrawings =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, ApproveDrawingRevision command) =>
        RolesThatMayManageDrawings.IncludesAny(user.Roles);
}
