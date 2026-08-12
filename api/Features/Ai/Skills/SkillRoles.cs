using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Ai.Skills;

internal static class SkillRoles
{
    /// <summary>
    /// Who may read and edit the assistant's skills. The whole point of the store is that the
    /// discipline owner (the MD) maintains doctrine himself, so this is the board plus
    /// administrators — mirror of the "AI Skills" sidebar row. Skills are commercial assets
    /// (reserve doctrine, fact patterns); they are NOT readable by the wider assistant audience,
    /// only exercised by it through the prompt.
    /// </summary>
    public static readonly RoleSet ManageSkills = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector);
}
