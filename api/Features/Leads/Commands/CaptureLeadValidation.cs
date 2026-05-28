using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class CaptureLeadValidation
{
    public ValidationOutcome Check(CaptureLead command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Reference)) errors.Add("Reference is required.");
        if (string.IsNullOrWhiteSpace(command.ContactName)) errors.Add("Contact name is required.");
        if (string.IsNullOrWhiteSpace(command.OwnerEmail)) errors.Add("Owner email is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
