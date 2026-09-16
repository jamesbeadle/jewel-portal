using Jewel.JPMS.Api.Features.Progress.Photos;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

public sealed class FileSitePhotosAuthorisation
{
    public bool Allows(SignedInUser user) => ProgressRoles.Contributors.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, FileSitePhotos command) => Allows(user);
}

public sealed class FileSitePhotosValidation
{
    public ValidationOutcome Check(FileSitePhotos command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProgressUpdateId)) errors.Add("ProgressUpdateId is required.");
        if (string.IsNullOrWhiteSpace(command.FiledByEmail)) errors.Add("Filing email is required.");
        if (command.SitePhotoIds is null || command.SitePhotoIds.Count == 0) errors.Add("At least one site photo id is required.");
        else if (command.SitePhotoIds.Count > ProgressPhotoLimits.MaxImagesPerBatch)
            errors.Add($"{command.SitePhotoIds.Count} photos were named; a batch files at most {ProgressPhotoLimits.MaxImagesPerBatch}.");
        else if (command.SitePhotoIds.Any(string.IsNullOrWhiteSpace)) errors.Add("Every site photo id must be given.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}

public sealed class DeleteSitePhotoAuthorisation
{
    public bool Allows(SignedInUser user) => ProgressRoles.Contributors.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, DeleteSitePhoto command) => Allows(user);
}
