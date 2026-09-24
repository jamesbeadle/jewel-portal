using Jewel.JPMS.Api.Features.Progress.Photos;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

public sealed class ArchiveSitePhotosAuthorisation
{
    public bool Allows(SignedInUser user) => ProgressRoles.Contributors.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, ArchiveSitePhotos command) => Allows(user);
}

public sealed class ArchiveSitePhotosValidation
{
    private const int NoteMaxLength = 1024;

    public ValidationOutcome Check(ArchiveSitePhotos command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ArchivedByEmail)) errors.Add("Archiving email is required.");
        errors.AddRange(PhotoErrors(command.Photos ?? Array.Empty<SitePhotoToArchive>()));
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }

    private static IEnumerable<string> PhotoErrors(IReadOnlyList<SitePhotoToArchive> photos)
    {
        if (photos.Count == 0) { yield return "At least one site photo is required."; yield break; }
        if (photos.Count > ProgressPhotoLimits.MaxImagesPerBatch)
        {
            yield return $"{photos.Count} photos were named; a batch archives at most {ProgressPhotoLimits.MaxImagesPerBatch}.";
            yield break;
        }
        var isAnIdMissing = photos.Any(photo => string.IsNullOrWhiteSpace(photo.SitePhotoId));
        var isANoteMissing = photos.Any(photo => string.IsNullOrWhiteSpace(photo.Note));
        var isANoteTooLong = photos.Any(photo => photo.Note?.Length > NoteMaxLength);
        if (isAnIdMissing) yield return "Every site photo id must be given.";
        if (isANoteMissing) yield return "Every archived photo needs a note saying why.";
        if (isANoteTooLong) yield return $"A note is at most {NoteMaxLength} characters.";
    }
}

public sealed class RestoreSitePhotoAuthorisation
{
    public bool Allows(SignedInUser user) => ProgressRoles.Contributors.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, RestoreSitePhoto command) => Allows(user);
}
