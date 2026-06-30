using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class CreateBidPackageFromMessageAuthorisation
{
    private static readonly RoleSet RolesThatMayCreatePackages =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator);

    public bool Allows(SignedInUser user, CreateBidPackageFromMessage command) => RolesThatMayCreatePackages.IncludesAny(user.Roles);
}
