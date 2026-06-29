using Jewel.JPMS.Api.Gates;

namespace Jewel.JPMS.Api.Features.Agents;

// The internal commercial roles permitted to operate agents: apply/remove them, chat, run analysis,
// decide proposals, and attempt a gated close. Admins carry every role server-side so they're in too.
internal static class AgentRoles
{
    public static readonly RoleSet AllowedToOperateAgents =
        RoleSet.Of(
            JpmsRoles.Director,
            JpmsRoles.ProjectManager,
            JpmsRoles.Estimator,
            JpmsRoles.SiteManager);
}
