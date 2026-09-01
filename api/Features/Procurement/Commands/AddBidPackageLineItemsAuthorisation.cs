using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class AddBidPackageLineItemsAuthorisation
{
    // Estimator (the QS) added 2026-07-27. The QS can create a bid package (CreateBidPackage), so
    // being refused the LINES of the package they had just created was an inconsistency between two
    // gates rather than a decision anyone took. Raising a variation with scope lines sends the
    // commands in one go, which is where it surfaced: package created, then 403 on the lines.
    // (Historically this pointed at AddBidPackageToVoq — removed 2026-08-12 when bid packages were
    // separated from the VO quoting process.)
    private static readonly RoleSet RolesThatMayEditLineItems = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.ProjectManager,
        JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, AddBidPackageLineItems command) => RolesThatMayEditLineItems.IncludesAny(user.Roles);
}
