using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class CreateBidPackageAuthorisation
{
    // Estimator (the QS) added 2026-07-27, in step with AddBidPackageLineItemsAuthorisation: the QS
    // may already create a package against a variation (AddBidPackageToVoq, gated on
    // VariationRoles.AllowedToManageVariations), so the standalone route into the same table should
    // not be the narrower one.
    private static readonly RoleSet RolesThatMayCreatePackages = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.ProjectManager,
        JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator);

    public bool Allows(SignedInUser user, CreateBidPackage command) => RolesThatMayCreatePackages.IncludesAny(user.Roles);
}
