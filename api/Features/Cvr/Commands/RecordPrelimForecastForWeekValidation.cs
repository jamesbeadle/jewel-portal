using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class RecordPrelimForecastForWeekValidation
{
    public ValidationOutcome Check(RecordPrelimForecastForWeek command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.PrelimDescription)) errors.Add("Prelim description is required.");
        if (command.WeekNumber <= 0) errors.Add("Week number must be greater than zero.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
