using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class ReviseValuationAuthorisation
{
    private static readonly RoleSet RolesThatMayReviseValuations =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, ReviseValuation command) => RolesThatMayReviseValuations.IncludesAny(user.Roles);
}
