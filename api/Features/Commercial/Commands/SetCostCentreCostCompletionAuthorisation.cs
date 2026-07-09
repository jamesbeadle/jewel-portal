using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SetCostCentreCostCompletionAuthorisation
{
    // Same roles that may set cost-code budgets: this is the commercial team's
    // assessment of cost-side progress, not a site-level entry.
    private static readonly RoleSet RolesThatMaySetCostCompletion =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, SetCostCentreCostCompletion command) =>
        RolesThatMaySetCostCompletion.IncludesAny(user.Roles);
}
