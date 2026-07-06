using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class ExtractQuoteFromMessageAuthorisation
{
    // Whoever manages the tender may extract submissions from its responses.
    private static readonly RoleSet RolesThatMayExtract =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator);

    public bool Allows(SignedInUser user, ExtractQuoteFromMessage command) => RolesThatMayExtract.IncludesAny(user.Roles);
}
