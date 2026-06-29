using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Agents;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class DecideAgentProposalValidation
{
    public ValidationOutcome Check(DecideAgentProposal command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProposalId)) errors.Add("ProposalId is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
