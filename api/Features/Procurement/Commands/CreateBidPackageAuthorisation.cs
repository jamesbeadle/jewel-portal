using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class CreateBidPackageAuthorisation
{
    // Estimator (the QS) added 2026-07-27, in step with AddBidPackageLineItemsAuthorisation. This
    // is now THE way a bid package is created (the VO-quoting route, AddBidPackageToVoq, was
    // removed 2026-08-12 when bid packages were separated from the variation process).
    private static readonly RoleSet RolesThatMayCreatePackages = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.ProjectManager,
        JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    public bool Allows(SignedInUser user, CreateBidPackage command) => RolesThatMayCreatePackages.IncludesAny(user.Roles);
}
