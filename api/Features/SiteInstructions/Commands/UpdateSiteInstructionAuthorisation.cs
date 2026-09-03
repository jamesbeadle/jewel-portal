using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Api.Features.SiteInstructions.Commands;

public sealed class UpdateSiteInstructionAuthorisation
{
    private static readonly RoleSet RolesThatMayUpdateSiteInstructions =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager);
    public bool Allows(SignedInUser user, UpdateSiteInstruction command) => RolesThatMayUpdateSiteInstructions.IncludesAny(user.Roles);
}
