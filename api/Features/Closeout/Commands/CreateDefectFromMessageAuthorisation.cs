using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

// Mirrors RaiseDefectAuthorisation minus the external roles: the Control Centre is an internal
// surface, so the client/architect (who may raise defects through their own pages) have no
// business raising one from the projects mailbox.
public sealed class CreateDefectFromMessageAuthorisation
{
    private static readonly RoleSet RolesThatMayRaiseDefectsFromMail =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager);
    public bool Allows(SignedInUser user, CreateDefectFromMessage command) =>
        RolesThatMayRaiseDefectsFromMail.IncludesAny(user.Roles);
}
