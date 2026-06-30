using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class InviteSubcontractorsToBidPackageAuthorisation
{
    private static readonly RoleSet RolesThatMayInvite =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator);

    public bool Allows(SignedInUser user, InviteSubcontractorsToBidPackage command) => RolesThatMayInvite.IncludesAny(user.Roles);
}
