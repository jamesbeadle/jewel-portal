using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SendBidPackageInviteAuthorisation
{
    // The composer's own circle, as BidPackageInviteComposerEndpoints has enforced since the send
    // moved in-app. It is WIDER than SendBidPackageInviteToTenderListAuthorisation by Admin and
    // the estimator, whatever that endpoint's summary says — replicated here exactly rather than
    // narrowed, because changing who may invite is not a refactor.
    internal static readonly RoleSet RolesThatMayInvite = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager,
        JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    public bool Allows(SignedInUser user, SendBidPackageInvite command) =>
        RolesThatMayInvite.IncludesAny(user.Roles);
}
