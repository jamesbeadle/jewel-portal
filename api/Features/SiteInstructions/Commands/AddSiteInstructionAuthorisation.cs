using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Api.Features.SiteInstructions.Commands;

// Site instructions are internal: the people who run the job instruct site. Same internal set as
// defect and inventory maintenance (Director/PM/SiteManager) — no external roles.
public sealed class AddSiteInstructionAuthorisation
{
    private static readonly RoleSet RolesThatMayInstructSite =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager);
    public bool Allows(SignedInUser user, AddSiteInstruction command) => RolesThatMayInstructSite.IncludesAny(user.Roles);
}
