using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

public sealed class RestoreAiSkillVersionAuthorisation
{
    public bool Allows(SignedInUser user, RestoreAiSkillVersion command) =>
        SkillRoles.ManageSkills.IncludesAny(user.Roles);
}
