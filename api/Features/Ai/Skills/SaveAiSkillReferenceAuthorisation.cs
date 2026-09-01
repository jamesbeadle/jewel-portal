using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

public sealed class SaveAiSkillReferenceAuthorisation
{
    public bool Allows(SignedInUser user, SaveAiSkillReference command) =>
        SkillRoles.ManageSkills.IncludesAny(user.Roles);
}
