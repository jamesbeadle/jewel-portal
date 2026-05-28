using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.CommercialInputs;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Commands;

public sealed class RecordSubcontractorRetentionValidation
{
    public ValidationOutcome Check(RecordSubcontractorRetention command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.SubcontractorReference)) errors.Add("Subcontractor reference is required.");
        if (command.RetentionPercent is < 0 or > 1) errors.Add("Retention percent must be a fraction between 0 and 1.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
