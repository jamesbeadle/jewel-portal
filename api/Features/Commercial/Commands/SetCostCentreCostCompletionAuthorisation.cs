using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SetCostCentreCostCompletionAuthorisation
{
    // The commercial team's assessment of cost-side progress, plus the Finance
    // Director (the Financials tab is a finance surface) and Role.Admin per the
    // newer authorisation convention.
    private static readonly RoleSet RolesThatMaySetCostCompletion =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, SetCostCentreCostCompletion command) =>
        RolesThatMaySetCostCompletion.IncludesAny(user.Roles);
}
