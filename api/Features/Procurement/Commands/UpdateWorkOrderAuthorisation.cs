using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class UpdateWorkOrderAuthorisation
{
    private static readonly RoleSet RolesThatMayUpdateWorkOrders =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator);

    public bool Allows(SignedInUser user, UpdateWorkOrder command) => RolesThatMayUpdateWorkOrders.IncludesAny(user.Roles);
}
