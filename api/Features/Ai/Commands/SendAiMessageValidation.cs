using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Commands;

public sealed class SendAiMessageValidation
{
    private const int MaxMessageLength = 8000;

    public ValidationOutcome Check(SendAiMessage command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Message)) errors.Add("Type a message first.");
        if (command.Message is { Length: > MaxMessageLength })
            errors.Add($"That message is too long ({MaxMessageLength} characters max).");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
