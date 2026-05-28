using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class CreateBidPackageAuthorisation
{
    private static readonly RoleSet RolesThatMayCreatePackages =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.ProcurementLead);

    public bool Allows(SignedInUser user, CreateBidPackage command) => RolesThatMayCreatePackages.Includes(user.Role);
}
