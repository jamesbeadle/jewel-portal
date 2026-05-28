using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class MarkLeadAsLostValidation
{
    public ValidationOutcome Check(MarkLeadAsLost command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        if (string.IsNullOrWhiteSpace(command.DecidedByEmail)) errors.Add("Decision-maker email is required.");
        if (string.IsNullOrWhiteSpace(command.Reason)) errors.Add("Reason is required for lost leads.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
