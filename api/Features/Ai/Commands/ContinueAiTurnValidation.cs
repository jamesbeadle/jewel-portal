using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Commands;

public sealed class ContinueAiTurnValidation
{
    public ValidationOutcome Check(ContinueAiTurn command)
    {
        if (string.IsNullOrWhiteSpace(command.ConversationId))
            return ValidationOutcome.Failed("There is no conversation to continue.");
        return ValidationOutcome.Passed;
    }
}
