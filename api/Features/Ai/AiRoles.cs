using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Ai;

internal static class AiRoles
{
    /// <summary>
    /// Who may talk to the assistant. Mirrors the client's <c>DesktopNavigation.CanUseAssistant</c> —
    /// administrators and the two directors. Deliberately the narrowest gate in the app: every
    /// message spends money on the Claude API, so it is not offered to anyone who cannot authorise
    /// that spend. Keep the two lists in step.
    /// </summary>
    public static readonly RoleSet AllowedToUseAssistant =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, Role.FinanceDirector);
}
