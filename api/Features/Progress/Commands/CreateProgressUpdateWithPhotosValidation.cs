using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

public sealed class CreateProgressUpdateWithPhotosValidation
{
    public ValidationOutcome Check(CreateProgressUpdateWithPhotos command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProgressUpdateId)) errors.Add("ProgressUpdateId is required.");
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("A title for the group of photos is required.");
        if (string.IsNullOrWhiteSpace(command.CreatedByEmail)) errors.Add("Creating email is required.");
        if (command.Photos.Count == 0) errors.Add("At least one photo is required.");
        NewProgressPhotoRules.Check(command.Photos, errors);
        ProgressWeatherRules.Check(command.Weather, errors);
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
