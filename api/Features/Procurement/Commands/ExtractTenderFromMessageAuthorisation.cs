using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Mirrors SaveExtractedQuote: whoever may commit a tender submission may run the extraction that
// proposes it.
public sealed class ExtractTenderFromMessageAuthorisation
{
    private static readonly RoleSet RolesThatMayExtract =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, ExtractTenderFromMessage command) => RolesThatMayExtract.IncludesAny(user.Roles);
}
