using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

public sealed class CreateProgressUpdateValidation
{
    public ValidationOutcome Check(CreateProgressUpdate command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProgressUpdateId)) errors.Add("ProgressUpdateId is required.");
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.Title)) errors.Add("A title for the group of photos is required.");
        if (string.IsNullOrWhiteSpace(command.CreatedByEmail)) errors.Add("Creating email is required.");
        if (command.Photos.Count == 0) errors.Add("At least one photo is required.");
        foreach (var photo in command.Photos)
        {
            if (string.IsNullOrWhiteSpace(photo.BlobRef)) errors.Add("Uploaded photo reference is required.");
            if (photo.FileSizeBytes <= 0) errors.Add($"Uploaded photo '{photo.FileName}' is empty.");
        }
        ProgressWeatherRules.Check(command.Weather, errors);
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
