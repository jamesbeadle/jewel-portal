using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

public sealed class AddProgressPhotosValidation
{
    public ValidationOutcome Check(AddProgressPhotos command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProgressUpdateId)) errors.Add("ProgressUpdateId is required.");
        if (string.IsNullOrWhiteSpace(command.UploadedByEmail)) errors.Add("Uploading email is required.");
        if (command.Photos.Count == 0) errors.Add("At least one photo is required.");
        NewProgressPhotoRules.Check(command.Photos, errors);
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
