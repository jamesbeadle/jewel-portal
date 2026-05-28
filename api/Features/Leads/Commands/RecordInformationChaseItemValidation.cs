using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class RecordInformationChaseItemValidation
{
    public ValidationOutcome Check(RecordInformationChaseItem command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        if (string.IsNullOrWhiteSpace(command.Kind)) errors.Add("Kind is required.");
        if (string.IsNullOrWhiteSpace(command.Description)) errors.Add("Description is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
