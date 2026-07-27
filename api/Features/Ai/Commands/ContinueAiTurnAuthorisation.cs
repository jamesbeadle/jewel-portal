using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Commands;

public sealed class ContinueAiTurnAuthorisation
{
    public bool Allows(SignedInUser user) => AiRoles.AllowedToUseAssistant.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, ContinueAiTurn command) => Allows(user);
}
