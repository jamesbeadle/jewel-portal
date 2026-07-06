using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SendBidPackageInviteAuthorisation
{
    // Same circle as inviting: whoever may invite may send the invite email.
    private static readonly RoleSet RolesThatMaySend =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator);

    public bool Allows(SignedInUser user, SendBidPackageInvite command) => RolesThatMaySend.IncludesAny(user.Roles);
}
