using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class UpdateManualWorkOrderAuthorisation
{
    // The same roles that may raise a manual order may correct one — editing is part of
    // the same reconciliation duty (see CreateManualWorkOrderAuthorisation).
    private static readonly RoleSet RolesThatMayEditOrders =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, UpdateManualWorkOrder command) =>
        RolesThatMayEditOrders.IncludesAny(user.Roles);
}
