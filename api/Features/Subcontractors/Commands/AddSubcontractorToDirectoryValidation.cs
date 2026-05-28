using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class AddSubcontractorToDirectoryValidation
{
    public ValidationOutcome Check(AddSubcontractorToDirectory command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.CompanyName)) errors.Add("Company name is required.");
        if (string.IsNullOrWhiteSpace(command.PrimaryTrade)) errors.Add("Primary trade is required.");
        if (string.IsNullOrWhiteSpace(command.ContactEmail)) errors.Add("Contact email is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
