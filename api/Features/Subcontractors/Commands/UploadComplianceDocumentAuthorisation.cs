using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class UploadComplianceDocumentAuthorisation
{
    private static readonly RoleSet InternalRolesThatMayUploadCompliance =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.OfficeComplianceCoordinator);

    /// <summary>Internal roles may upload to any record; a portal-scoped subcontractor login may
    /// only upload to its own record (SubcontractorScope) — never another company's.</summary>
    public bool Allows(SignedInUser user, UploadComplianceDocument command)
    {
        if (InternalRolesThatMayUploadCompliance.IncludesAny(user.Roles)) return true;

        var ownSubcontractorId = SubcontractorScope.OwnSubcontractorId(user);
        return ownSubcontractorId is not null
            && string.Equals(ownSubcontractorId, command.SubcontractorId, StringComparison.OrdinalIgnoreCase);
    }
}
