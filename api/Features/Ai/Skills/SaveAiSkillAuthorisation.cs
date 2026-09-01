using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

public sealed class SaveAiSkillAuthorisation
{
    public bool Allows(SignedInUser user, SaveAiSkill command) =>
        SkillRoles.ManageSkills.IncludesAny(user.Roles);
}
