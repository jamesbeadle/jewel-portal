using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SetBidPackageLineItemCoverageAuthorisation
{
    // Coverage mapping is a commercial decision — same roles that may edit the line items themselves,
    // plus the QS/Estimator who owns tender-to-BoQ reconciliation.
    private static readonly RoleSet RolesThatMaySetCoverage =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator);

    public bool Allows(SignedInUser user, SetBidPackageLineItemCoverage command) => RolesThatMaySetCoverage.IncludesAny(user.Roles);
}
