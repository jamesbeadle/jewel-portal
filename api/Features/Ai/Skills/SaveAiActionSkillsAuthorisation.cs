using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

public sealed class SaveAiActionSkillsAuthorisation
{
    public bool Allows(SignedInUser user, SaveAiActionSkills command) =>
        SkillRoles.ManageSkills.IncludesAny(user.Roles);
}
