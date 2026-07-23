using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class UpdateSubcontractorValidation
{
    public ValidationOutcome Check(UpdateSubcontractor command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.SubcontractorId)) errors.Add("SubcontractorId is required.");
        if (string.IsNullOrWhiteSpace(command.CompanyName)) errors.Add("Company name is required.");
        // Trades are only required for subcontractors/suppliers; the command doesn't carry the
        // category, so that check lives in the handler where the record is loaded.
        // Contact email and phone are optional — a directory record only needs a company name.
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
