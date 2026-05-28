using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class IssueProposalValidation
{
    public ValidationOutcome Check(IssueProposal command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        if (command.Value <= 0) errors.Add("Proposal value must be positive.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
