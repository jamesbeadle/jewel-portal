using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CostCenters;

namespace Jewel.JPMS.Api.Features.CostCenters.Commands;

public sealed class AddCostCenterAuthorisation
{
    // The cost-code master is a commercial control: directors and the QS team
    // manage it. Master admins pass any role gate.
    private static readonly RoleSet RolesThatMayManageCostCenters =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, AddCostCenter command) => RolesThatMayManageCostCenters.IncludesAny(user.Roles);
}
