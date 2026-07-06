using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class AddBidPackageLineItemsAuthorisation
{
    private static readonly RoleSet RolesThatMayEditLineItems =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator);

    public bool Allows(SignedInUser user, AddBidPackageLineItems command) => RolesThatMayEditLineItems.IncludesAny(user.Roles);
}
