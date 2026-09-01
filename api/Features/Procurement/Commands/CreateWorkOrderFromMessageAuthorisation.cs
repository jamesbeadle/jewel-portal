using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class CreateWorkOrderFromMessageAuthorisation
{
    // Raising a work order from the Control Centre is the same commitment as raising one from
    // the Work Orders tab, so the role set mirrors CreateManualWorkOrderAuthorisation exactly —
    // the door the order came in through doesn't change who may open it.
    private static readonly RoleSet RolesThatMayRaiseOrders =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, CreateWorkOrderFromMessage command) =>
        RolesThatMayRaiseOrders.IncludesAny(user.Roles);
}
