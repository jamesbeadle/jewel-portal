using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SetCostCodeBudgetAuthorisation
{
    private static readonly RoleSet RolesThatMaySetBudgets =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, SetCostCodeBudget command) =>
        RolesThatMaySetBudgets.IncludesAny(user.Roles);
}
