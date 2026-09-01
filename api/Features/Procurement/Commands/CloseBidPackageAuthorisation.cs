using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class CloseBidPackageAuthorisation
{
    // Closing ends the tender process, so it carries the award gate's roles — the other act that
    // ends a package — not the wider create/edit set.
    private static readonly RoleSet RolesThatMayClosePackages =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, CloseBidPackage command) => RolesThatMayClosePackages.IncludesAny(user.Roles);
}
