using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class ApproveWorkOrderAuthorisation
{
    // The same roles that may raise an order directly (CreateManualWorkOrderAuthorisation):
    // whoever could have raised the order released may approve the draft — including
    // whoever drafted it.
    private static readonly RoleSet RolesThatMayApproveOrders =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, ApproveWorkOrder command) =>
        RolesThatMayApproveOrders.IncludesAny(user.Roles);
}
