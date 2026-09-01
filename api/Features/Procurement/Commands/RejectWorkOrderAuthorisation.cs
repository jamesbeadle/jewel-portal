using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class RejectWorkOrderAuthorisation
{
    // The same roles that may raise or approve orders: rejecting a draft is the other
    // half of the same decision.
    private static readonly RoleSet RolesThatMayRejectOrders =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, RejectWorkOrder command) =>
        RolesThatMayRejectOrders.IncludesAny(user.Roles);
}
