using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Agents;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class AssignAgentAuthorisation
{
    public bool Allows(SignedInUser user, AssignAgent command) => AgentRoles.AllowedToOperateAgents.IncludesAny(user.Roles);
}
