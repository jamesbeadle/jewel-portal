using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class CreateCostCentreGroupAuthorisation
{
    // Same commercial roles that shape the Financials tab's other inputs (budgets,
    // cost completion) — grouping changes what everyone on the project sees.
    private static readonly RoleSet RolesThatMayManageGroups =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, CreateCostCentreGroup command) =>
        RolesThatMayManageGroups.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, RemoveCostCentreGroup command) =>
        RolesThatMayManageGroups.IncludesAny(user.Roles);
}
