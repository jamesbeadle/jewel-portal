using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Commands;

public sealed class CollectAiReplyValidation
{
    public ValidationOutcome Check(CollectAiReply command)
    {
        if (string.IsNullOrWhiteSpace(command.ConversationId))
            return ValidationOutcome.Failed("There is no conversation to collect for.");
        if (string.IsNullOrWhiteSpace(command.ReplyId))
            return ValidationOutcome.Failed("There is no reply to collect.");
        return ValidationOutcome.Passed;
    }
}
