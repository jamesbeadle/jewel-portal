namespace Jewel.JPMS.Api.Features.Progress.Photos;

/// <summary>Reads the image files off a multipart form into memory, in the order they were posted,
/// and refuses a batch over the limit before any file is opened.</summary>
internal static class ProgressPhotoFormReader
{
    public sealed record Read(IReadOnlyList<IncomingProgressPhoto> Images, string? Refusal);

    public static async Task<Read> ReadAsync(IFormCollection form, CancellationToken cancellationToken)
    {
        var files = form.Files.Where(file => file.Length > 0).ToList();
        if (files.Count == 0) return new Read(Array.Empty<IncomingProgressPhoto>(), "At least one photo is required.");
        if (files.Count > ProgressPhotoLimits.MaxImagesPerBatch)
            return new Read(Array.Empty<IncomingProgressPhoto>(),
                $"{files.Count} images were posted; a batch takes at most {ProgressPhotoLimits.MaxImagesPerBatch}.");

        var images = new List<IncomingProgressPhoto>(files.Count);
        foreach (var file in files)
        {
            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            var fileName = string.IsNullOrWhiteSpace(file.FileName) ? "photo" : file.FileName;
            images.Add(new IncomingProgressPhoto(fileName, file.ContentType, buffer.ToArray()));
        }
        return new Read(images, null);
    }
}
