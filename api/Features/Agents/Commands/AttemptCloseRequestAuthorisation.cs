using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Agents;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class AttemptCloseRequestAuthorisation
{
    public bool Allows(SignedInUser user, AttemptCloseRequest command) => AgentRoles.AllowedToOperateAgents.IncludesAny(user.Roles);
}
