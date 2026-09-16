using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

public sealed class CreateProgressUpdateValidation
{
    public ValidationOutcome Check(CreateProgressUpdate command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("A title is required.");
        if (string.IsNullOrWhiteSpace(command.Description))
            errors.Add("A description is required — a day with nothing recorded should have no update, not a blank one.");
        if (command.WorkDate == default) errors.Add("The date of the works is required.");
        if (string.IsNullOrWhiteSpace(command.CreatedByEmail)) errors.Add("Creating email is required.");
        ProgressWeatherRules.Check(command.Weather, errors);
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
