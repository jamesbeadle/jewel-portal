using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class ReviseProposalValidation
{
    public ValidationOutcome Check(ReviseProposal command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        if (command.RevisedValue <= 0) errors.Add("Revised value must be positive.");
        if (string.IsNullOrWhiteSpace(command.Notes)) errors.Add("Notes are required for a revision.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
