using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class ReopenBidPackageAuthorisation
{
    // Reopening undoes a close, so it carries the same roles as closing.
    private static readonly RoleSet RolesThatMayReopenPackages =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, ReopenBidPackage command) => RolesThatMayReopenPackages.IncludesAny(user.Roles);
}
