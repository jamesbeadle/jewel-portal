using Jewel.JPMS.Api.Gates;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class InviteSubcontractorPortalUserAuthorisation
{
    // Mirrors the scoping doc (docs/06-backlog/subcontractor-crm-scope.md §7): portal invites are
    // sent by the office/compliance coordinator, project managers and the director.
    private static readonly RoleSet RolesThatMayInvite =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator);

    public bool Allows(SignedInUser user) => RolesThatMayInvite.IncludesAny(user.Roles);
}
