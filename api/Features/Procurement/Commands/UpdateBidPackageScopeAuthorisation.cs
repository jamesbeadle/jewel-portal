using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class UpdateBidPackageScopeAuthorisation
{
    private static readonly RoleSet RolesThatMayEditPackages =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.ProcurementLead);

    public bool Allows(SignedInUser user, UpdateBidPackageScope command) => RolesThatMayEditPackages.Includes(user.Role);
}
