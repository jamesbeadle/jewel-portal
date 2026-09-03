using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Api.Features.SiteInstructions.Commands;

// Mirrors AddSiteInstructionAuthorisation: the Control Centre is an internal surface and the
// raise roles are already internal-only, so the from-mail set is the same.
public sealed class CreateSiteInstructionFromMessageAuthorisation
{
    private static readonly RoleSet RolesThatMayInstructSiteFromMail =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager);
    public bool Allows(SignedInUser user, CreateSiteInstructionFromMessage command) =>
        RolesThatMayInstructSiteFromMail.IncludesAny(user.Roles);
}
