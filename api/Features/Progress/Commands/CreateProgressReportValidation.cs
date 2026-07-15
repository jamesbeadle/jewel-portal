using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

public sealed class CreateProgressReportValidation
{
    public ValidationOutcome Check(CreateProgressReport command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("A report title is required.");
        if (command.PeriodStart is { } start && command.PeriodEnd is { } end && end < start)
            errors.Add("The period end cannot be before the period start.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
