using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class GenerateBidPackageDraftAuthorisation
{
    // Whoever manages the tender may ask for a draft — same set as the other package commands.
    private static readonly RoleSet RolesThatMayGenerate =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, GenerateBidPackageDraft command) => RolesThatMayGenerate.IncludesAny(user.Roles);
}
