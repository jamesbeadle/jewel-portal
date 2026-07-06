using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class ImportDrawingFromMessageAuthorisation
{
    // Same circle as registering drawings, plus the compliance coordinator who runs triage.
    private static readonly RoleSet RolesThatMayImport =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator);

    public bool Allows(SignedInUser user, ImportDrawingFromMessage command) => RolesThatMayImport.IncludesAny(user.Roles);
}
