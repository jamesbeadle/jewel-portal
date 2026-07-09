using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SetCostCentreFinalisationAuthorisation
{
    // Locking a centre changes how its remaining funds are read by everyone on the
    // project — same commercial roles as the Financials tab's other inputs.
    private static readonly RoleSet RolesThatMayFinalise =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, SetCostCentreFinalisation command) =>
        RolesThatMayFinalise.IncludesAny(user.Roles);
}
