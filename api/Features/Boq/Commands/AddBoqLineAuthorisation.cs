using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Boq;

namespace Jewel.JPMS.Api.Features.Boq.Commands;

public sealed class AddBoqLineAuthorisation
{
    private static readonly RoleSet RolesThatMayEditBoq =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, AddBoqLine command) => RolesThatMayEditBoq.Includes(user.Role);
}
