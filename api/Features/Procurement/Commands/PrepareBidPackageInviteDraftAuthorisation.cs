using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class PrepareBidPackageInviteDraftAuthorisation
{
    // Same circle as inviting: whoever may invite may draft the invite email.
    private static readonly RoleSet RolesThatMayDraft =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator);

    public bool Allows(SignedInUser user, PrepareBidPackageInviteDraft command) => RolesThatMayDraft.IncludesAny(user.Roles);
}
