using Jewel.JPMS.Api.Gates;

namespace Jewel.JPMS.Api.Features.Ai;

internal static class AiRoles
{
    /// <summary>
    /// Who may talk to the assistant. Mirrors the client's <c>DesktopNavigation.CanUseAssistant</c> —
    /// administrators and the commercial team. Keep the two lists in step.
    ///
    /// <para>Widened from the two directors on 2026-07-27: the assistant drafts variations from RFI
    /// correspondence into the Create Variation Order Quote dialog, and that is PM and QS work
    /// (<c>VariationRoles.AllowedToManageVariations</c>). Every tool in the catalogue is a read
    /// whose backing query already admits these roles, so this grants nothing they could not
    /// already reach by clicking — see the note on <c>AiToolCatalogue</c>.</para>
    ///
    /// <para>Spend is still controlled, by the panel's per-user cost acknowledgement and by the
    /// AgentActivity row written for every turn — not by the role gate alone.</para>
    /// </summary>
    public static readonly RoleSet AllowedToUseAssistant = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator);
}
