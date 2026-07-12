using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SetCostCentreFinalisationAuthorisation
{
    // Locking a centre changes how its remaining funds are read by everyone on the
    // project — the commercial roles plus the Finance Director (realising profit is
    // a finance call) and Role.Admin per the newer authorisation convention.
    private static readonly RoleSet RolesThatMayFinalise =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, SetCostCentreFinalisation command) =>
        RolesThatMayFinalise.IncludesAny(user.Roles);
}
