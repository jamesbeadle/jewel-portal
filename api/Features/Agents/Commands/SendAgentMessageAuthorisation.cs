using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Agents;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class SendAgentMessageAuthorisation
{
    public bool Allows(SignedInUser user, SendAgentMessage command) => AgentRoles.AllowedToOperateAgents.IncludesAny(user.Roles);
}
