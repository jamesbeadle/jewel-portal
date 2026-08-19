using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class PromoteSubcontractorToDirectoryValidation
{
    public ValidationOutcome Check(PromoteSubcontractorToDirectory command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.SubcontractorId)) errors.Add("SubcontractorId is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
