using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SendBidPackageInviteToTenderListAuthorisation
{
    // Same circle as inviting: whoever may invite may draft the invite email.
    private static readonly RoleSet RolesThatMayDraft =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    public bool Allows(SignedInUser user, SendBidPackageInviteToTenderList command) => RolesThatMayDraft.IncludesAny(user.Roles);
}
