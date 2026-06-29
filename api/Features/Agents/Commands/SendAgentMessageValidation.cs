using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Agents;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class SendAgentMessageValidation
{
    private readonly AgentRegistry registry;
    public SendAgentMessageValidation(AgentRegistry registry) { this.registry = registry; }

    public ValidationOutcome Check(SendAgentMessage command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.RequestId)) errors.Add("RequestId is required.");
        if (string.IsNullOrWhiteSpace(command.AgentKey)) errors.Add("AgentKey is required.");
        else if (!registry.Exists(command.AgentKey)) errors.Add($"Unknown agent '{command.AgentKey}'.");
        if (string.IsNullOrWhiteSpace(command.Body)) errors.Add("Message body is required.");
        if (string.IsNullOrWhiteSpace(command.AuthorEmail)) errors.Add("Author email is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
