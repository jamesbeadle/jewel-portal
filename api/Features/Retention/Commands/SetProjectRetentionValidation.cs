using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Retention;

namespace Jewel.JPMS.Api.Features.Retention.Commands;

public sealed class SetProjectRetentionValidation
{
    public ValidationOutcome Check(SetProjectRetention command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (command.RetentionPercent is < 0 or > 100) errors.Add("Retention percent must be between 0 and 100.");
        if (command.CompletionReleasePercent is < 0 or > 100) errors.Add("Completion release percent must be between 0 and 100.");
        if (command.CompletionReleasePercent > command.RetentionPercent)
            errors.Add("Completion release percent cannot exceed the retention percent.");
        if (command.DefectsPeriodMonths is not (6 or 12)) errors.Add("Defects period must be 6 or 12 months.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
