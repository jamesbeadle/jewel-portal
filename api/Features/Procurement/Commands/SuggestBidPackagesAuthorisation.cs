using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SuggestBidPackagesAuthorisation
{
    // Exactly the people who may CREATE a bid package — a suggestion is only ever a prelude to
    // creating one, and it reads the whole valuation report, which is not for wider eyes.
    private static readonly RoleSet RolesThatMayCreatePackages = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.ProjectManager,
        JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, SuggestBidPackages command) => RolesThatMayCreatePackages.IncludesAny(user.Roles);
}
