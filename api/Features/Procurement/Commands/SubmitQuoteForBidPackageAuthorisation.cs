using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SubmitQuoteForBidPackageAuthorisation
{
    private static readonly RoleSet RolesThatMaySubmitQuotes =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.Subcontractor);

    public bool Allows(SignedInUser user, SubmitQuoteForBidPackage command) => RolesThatMaySubmitQuotes.IncludesAny(user.Roles);
}
