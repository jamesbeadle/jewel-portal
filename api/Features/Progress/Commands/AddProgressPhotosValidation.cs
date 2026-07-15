using Jewel.JPMS.Api.Cqrs;
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
        foreach (var photo in command.Photos)
        {
            if (string.IsNullOrWhiteSpace(photo.BlobRef)) errors.Add("Uploaded photo reference is required.");
            if (photo.FileSizeBytes <= 0) errors.Add($"Uploaded photo '{photo.FileName}' is empty.");
        }
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
