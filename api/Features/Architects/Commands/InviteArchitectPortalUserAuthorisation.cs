namespace Jewel.JPMS.Api.Features.Architects.Commands;

public sealed class InviteArchitectPortalUserAuthorisation
{
    private static readonly RoleSet RolesThatMayInvite =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager,
            JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    public bool Allows(SignedInUser user) => RolesThatMayInvite.IncludesAny(user.Roles);
}
