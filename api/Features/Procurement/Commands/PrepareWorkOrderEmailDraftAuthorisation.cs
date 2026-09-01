using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class PrepareWorkOrderEmailDraftAuthorisation
{
    private static readonly RoleSet RolesThatMayEmailWorkOrders =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, PrepareWorkOrderEmailDraft command) => RolesThatMayEmailWorkOrders.IncludesAny(user.Roles);
}
