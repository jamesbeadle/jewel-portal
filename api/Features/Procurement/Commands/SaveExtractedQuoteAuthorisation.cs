using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SaveExtractedQuoteAuthorisation
{
    private static readonly RoleSet RolesThatMaySave =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    public bool Allows(SignedInUser user, SaveExtractedQuote command) => RolesThatMaySave.IncludesAny(user.Roles);
}
