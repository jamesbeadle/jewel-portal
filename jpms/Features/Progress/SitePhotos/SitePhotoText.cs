using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Features.Progress.SitePhotos;

/// <summary>The pool's sentences — the header strapline, a photo's standing, what a drop did.</summary>
public static class SitePhotoText
{
    public static string Summary(IReadOnlyList<SitePhoto> photos)
    {
        var unfiled = photos.Count(photo => !photo.IsFiled);
        return $"{photos.Count} photograph{Plural(photos.Count)} in the pool · {unfiled} waiting to be filed.";
    }

    public static string Standing(SitePhoto photo) =>
        photo.IsFiled
            ? $"Filed {DateText(photo.FiledAt)} onto progress update {photo.FiledToProgressUpdateId} · dropped by {photo.UploadedByEmail} {DateText(photo.UploadedAt)}"
            : $"Unfiled · dropped by {photo.UploadedByEmail} {DateText(photo.UploadedAt)} · {FormatSize(photo.FileSizeBytes)}";

    public static string UploadSummary(SitePhotoUploadResult upload)
    {
        var parts = new List<string> { $"{upload.StoredCount} stored" };
        if (upload.DuplicateCount > 0) parts.Add($"{upload.DuplicateCount} already in the pool");
        if (upload.FailedCount > 0)
        {
            var failed = upload.Outcomes.Where(outcome => outcome.Result == ProgressPhotoIntakeResult.Failed)
                .Select(outcome => $"{outcome.FileName} ({outcome.Detail})");
            parts.Add($"{upload.FailedCount} failed: {string.Join("; ", failed)}");
        }
        return string.Join(" · ", parts) + ".";
    }

    private static string Plural(int count) => count == 1 ? "" : "s";
}
