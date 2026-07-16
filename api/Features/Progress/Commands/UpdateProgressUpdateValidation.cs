using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

public sealed class UpdateProgressUpdateValidation
{
    public ValidationOutcome Check(UpdateProgressUpdate command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProgressUpdateId)) errors.Add("ProgressUpdateId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("A title is required.");
        ProgressWeatherRules.Check(command.Weather, errors);
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
