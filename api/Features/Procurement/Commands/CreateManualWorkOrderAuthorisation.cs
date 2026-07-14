using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class CreateManualWorkOrderAuthorisation
{
    // The commercial roles that shape the Financials tab, plus the Finance Director —
    // raising an order directly is part of reconciling historical commitments — and
    // Role.Admin per the newer convention.
    private static readonly RoleSet RolesThatMayRaiseOrders =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, CreateManualWorkOrder command) =>
        RolesThatMayRaiseOrders.IncludesAny(user.Roles);
}
