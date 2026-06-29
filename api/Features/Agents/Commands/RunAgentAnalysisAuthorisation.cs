using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Agents;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class RunAgentAnalysisAuthorisation
{
    public bool Allows(SignedInUser user, RunAgentAnalysis command) => AgentRoles.AllowedToOperateAgents.IncludesAny(user.Roles);
}
