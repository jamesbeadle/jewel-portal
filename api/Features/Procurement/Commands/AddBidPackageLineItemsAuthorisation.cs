using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class AddBidPackageLineItemsAuthorisation
{
    // Estimator (the QS) added 2026-07-27. VariationRoles.AllowedToManageVariations already lets the
    // QS raise a variation and hang a bid package off it (AddBidPackageToVoq), so being refused the
    // LINES of the package they had just created was an inconsistency between two gates rather than
    // a decision anyone took. Raising a variation with scope lines sends all three commands in one
    // go, which is where it surfaced: variation created, empty package created, 403 on the lines.
    private static readonly RoleSet RolesThatMayEditLineItems = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.ProjectManager,
        JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, AddBidPackageLineItems command) => RolesThatMayEditLineItems.IncludesAny(user.Roles);
}
