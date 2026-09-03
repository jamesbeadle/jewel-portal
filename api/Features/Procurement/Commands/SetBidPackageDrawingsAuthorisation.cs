using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SetBidPackageDrawingsAuthorisation
{
    private static readonly RoleSet RolesThatMayLink =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    public bool Allows(SignedInUser user, SetBidPackageDrawings command) => RolesThatMayLink.IncludesAny(user.Roles);
}
