using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Agents;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class RemoveRequestAgentAuthorisation
{
    public bool Allows(SignedInUser user, RemoveRequestAgent command) => AgentRoles.AllowedToOperateAgents.IncludesAny(user.Roles);
}
