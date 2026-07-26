using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Commands;

public sealed class SendAiMessageAuthorisation
{
    public bool Allows(SignedInUser user) => AiRoles.AllowedToUseAssistant.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, SendAiMessage command) => Allows(user);
}
