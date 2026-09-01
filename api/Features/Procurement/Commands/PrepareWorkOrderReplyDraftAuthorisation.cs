using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// A reply draft stages an external communication in the shared mailbox, exactly like drafting the
// fresh work-order email — so it carries the same gate as PrepareWorkOrderEmailDraftAuthorisation.
public sealed class PrepareWorkOrderReplyDraftAuthorisation
{
    private static readonly RoleSet RolesThatMayEmailWorkOrders =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, PrepareWorkOrderReplyDraft command) => RolesThatMayEmailWorkOrders.IncludesAny(user.Roles);
}
