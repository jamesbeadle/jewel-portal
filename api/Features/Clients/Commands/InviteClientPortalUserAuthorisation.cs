
namespace Jewel.JPMS.Api.Features.Clients.Commands;

public sealed class InviteClientPortalUserAuthorisation
{
    // Client portal invites are sent by whoever manages client accounts (ClientRoles), plus the
    // office roles that send subcontractor portal invites — the same back-office task.
    private static readonly RoleSet RolesThatMayInvite =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager,
            JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    public bool Allows(SignedInUser user) => RolesThatMayInvite.IncludesAny(user.Roles);
}
