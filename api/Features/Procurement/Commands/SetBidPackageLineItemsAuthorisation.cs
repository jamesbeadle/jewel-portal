using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SetBidPackageLineItemsAuthorisation
{
    private static readonly RoleSet RolesThatMayEditLineItems =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, SetBidPackageLineItems command) => RolesThatMayEditLineItems.IncludesAny(user.Roles);
}
