using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

/// <summary>How a progress update row and a photo row are built from a command — the one place
/// the two create commands and the photo batch agree on what a new row carries.</summary>
internal static class ProgressUpdateRows
{
    public static ProgressUpdateEntity New(
        string progressUpdateId, string projectId, string title, string description,
        DateTimeOffset? workDate, ProgressWeather? weather, string createdByEmail, DateTimeOffset createdAt) =>
        new()
        {
            ProgressUpdateId = progressUpdateId,
            ProjectId = projectId,
            Title = title.Trim(),
            Description = description.Trim(),
            WorkDate = workDate,
            WeatherSummary = weather?.Summary?.Trim() ?? "",
            WeatherObservedAt = weather?.ObservedAt,
            WeatherTempHighC = weather?.TempHighC,
            WeatherTempLowC = weather?.TempLowC,
            WeatherWindMph = weather?.WindMph,
            WeatherHumidityPercent = weather?.HumidityPercent,
            WeatherPrecipInches = weather?.PrecipInches,
            CreatedByEmail = createdByEmail,
            CreatedAt = createdAt
        };
}

internal static class ProgressPhotoRows
{
    public static ProgressPhotoEntity New(
        NewProgressPhoto photo, string progressUpdateId, string projectId, int sortOrder,
        string uploadedByEmail, DateTimeOffset uploadedAt) =>
        new()
        {
            ProgressPhotoId = photo.ProgressPhotoId,
            ProgressUpdateId = progressUpdateId,
            ProjectId = projectId,
            FileName = photo.FileName,
            BlobRef = photo.BlobRef,
            ContentType = photo.ContentType,
            FileSizeBytes = photo.FileSizeBytes,
            SortOrder = sortOrder,
            ContentHash = photo.ContentHash,
            UploadedByEmail = uploadedByEmail,
            UploadedAt = uploadedAt
        };
}
