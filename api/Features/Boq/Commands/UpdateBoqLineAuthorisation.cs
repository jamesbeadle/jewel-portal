using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Boq;

namespace Jewel.JPMS.Api.Features.Boq.Commands;

public sealed class UpdateBoqLineAuthorisation
{
    private static readonly RoleSet RolesThatMayEditBoq =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, UpdateBoqLine command) => RolesThatMayEditBoq.Includes(user.Role);
}
