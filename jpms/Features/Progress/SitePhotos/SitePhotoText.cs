using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Features.Progress.SitePhotos;

/// <summary>The pool's sentences — the header strapline, a photo's standing, what a drop did.</summary>
public static class SitePhotoText
{
    private const string WorkDayFormat = "ddd d MMM yyyy";

    public static string Summary(IReadOnlyList<SitePhoto> photos, Project? project)
    {
        var unfiled = photos.Count(photo => photo.IsWaiting);
        var waiting = $"{unfiled} waiting to be filed";
        if (project is null) return $"{photos.Count} photograph{Plural(photos.Count)} in the pool · {waiting}.";
        var filed = photos.Count(photo => photo.IsFiledTo(project.ProjectId));
        return $"{filed} photograph{Plural(filed)} filed to {project.Reference} {project.Name} · {waiting} across the pool.";
    }

    /// <summary>"JBB-2026-001 · Mon 21 Sep 2026" — the project and the day the photograph shows.</summary>
    public static string Destination(SitePhotoDestination destination)
    {
        var day = destination.WorkDate is { } workDate ? workDate.ToString(WorkDayFormat) : destination.ProgressUpdateTitle;
        return $"{destination.ProjectReference} · {day}";
    }

    public static string DestinationHref(SitePhoto photo) =>
        $"/projects/{photo.FiledToProjectId}/progress#update-{photo.FiledToProgressUpdateId}";

    public static string Standing(SitePhoto photo)
    {
        var dropped = $"dropped by {photo.UploadedByEmail} {DateText(photo.UploadedAt)}";
        if (photo.FiledTo is { } destination)
        {
            return $"Filed {DateText(photo.FiledAt)} to {destination.ProjectReference} {destination.ProjectName}, the update of "
                + $"{Destination(destination)} ({destination.ProgressUpdateTitle}) · {dropped}";
        }
        if (photo.IsFiled) return $"Filed {DateText(photo.FiledAt)} onto progress update {photo.FiledToProgressUpdateId} · {dropped}";
        if (photo.Archive is { } archive) return $"{ArchiveReading(archive)} · {dropped}";
        return $"Unfiled · {dropped} · {FormatSize(photo.FileSizeBytes)}";
    }

    public static string ArchiveReading(SitePhotoArchive archive)
    {
        var week = archive.PeriodEnd is { } periodEnd ? $" for the week ending {DateText(periodEnd)}" : "";
        return $"Archived {DateText(archive.ArchivedAt)}{week} · {archive.Reason.Label()}: {archive.Note}";
    }

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
