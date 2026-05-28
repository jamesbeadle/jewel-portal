using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class AwardBidPackageAuthorisation
{
    private static readonly RoleSet RolesThatMayAwardPackages =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, AwardBidPackage command) => RolesThatMayAwardPackages.Includes(user.Role);
}
