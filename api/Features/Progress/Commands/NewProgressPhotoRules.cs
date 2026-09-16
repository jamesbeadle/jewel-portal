using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

/// <summary>What every stored-and-waiting photo must carry before its row is written.</summary>
internal static class NewProgressPhotoRules
{
    public static void Check(IReadOnlyList<NewProgressPhoto> photos, List<string> errors)
    {
        foreach (var photo in photos)
        {
            if (string.IsNullOrWhiteSpace(photo.BlobRef)) errors.Add("Uploaded photo reference is required.");
            if (photo.FileSizeBytes <= 0) errors.Add($"Uploaded photo '{photo.FileName}' is empty.");
            if (string.IsNullOrWhiteSpace(photo.ContentHash)) errors.Add($"Uploaded photo '{photo.FileName}' has no content hash.");
        }
    }
}
