using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class UploadComplianceDocumentAuthorisation
{
    private static readonly RoleSet RolesThatMayUploadCompliance =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.Subcontractor);

    public bool Allows(SignedInUser user, UploadComplianceDocument command) => RolesThatMayUploadCompliance.IncludesAny(user.Roles);
}
