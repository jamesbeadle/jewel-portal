using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Agents;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class DecideAgentProposalAuthorisation
{
    public bool Allows(SignedInUser user, DecideAgentProposal command) => AgentRoles.AllowedToOperateAgents.IncludesAny(user.Roles);
}
