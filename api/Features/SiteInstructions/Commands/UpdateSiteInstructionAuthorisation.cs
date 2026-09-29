using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Api.Features.SiteInstructions.Commands;

public sealed class UpdateSiteInstructionAuthorisation
{
    public bool Allows(SignedInUser user, UpdateSiteInstruction command) => SiteInstructionRoles.Managers.IncludesAny(user.Roles);
}
