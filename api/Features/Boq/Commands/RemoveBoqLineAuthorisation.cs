using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Boq;

namespace Jewel.JPMS.Api.Features.Boq.Commands;

public sealed class RemoveBoqLineAuthorisation
{
    private static readonly RoleSet RolesThatMayRemoveBoqLines =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, RemoveBoqLine command) => RolesThatMayRemoveBoqLines.Includes(user.Role);
}
