using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Progress;

internal static class ProgressEntityMapping
{
    public static ProgressUpdate ToModel(this ProgressUpdateEntity entity, IReadOnlyList<ProgressPhoto> photos) =>
        new(entity.ProgressUpdateId, entity.ProjectId, entity.Title, entity.Description,
            entity.WorkDate, entity.ToWeatherModel(), entity.CreatedByEmail, entity.CreatedAt, photos);

    /// <summary>Null when nothing was recorded — a blank summary with all-null figures.</summary>
    public static ProgressWeather? ToWeatherModel(this ProgressUpdateEntity entity) =>
        string.IsNullOrWhiteSpace(entity.WeatherSummary)
        && entity.WeatherObservedAt is null
        && entity.WeatherTempHighC is null && entity.WeatherTempLowC is null
        && entity.WeatherWindMph is null && entity.WeatherHumidityPercent is null
        && entity.WeatherPrecipInches is null
            ? null
            : new ProgressWeather(
                entity.WeatherSummary, entity.WeatherObservedAt,
                entity.WeatherTempHighC, entity.WeatherTempLowC,
                entity.WeatherWindMph, entity.WeatherHumidityPercent,
                entity.WeatherPrecipInches);

    public static ProgressPhoto ToModel(this ProgressPhotoEntity entity) =>
        new(entity.ProgressPhotoId, entity.ProgressUpdateId, entity.FileName, entity.ContentType,
            entity.FileSizeBytes, entity.SortOrder, entity.UploadedByEmail, entity.UploadedAt);

    public static ProgressReport ToModel(this ProgressReportEntity entity, IReadOnlyList<string> selectedUpdateIds) =>
        new(entity.ProgressReportId, entity.ProjectId, entity.Title, entity.PeriodStart, entity.PeriodEnd,
            entity.Introduction, entity.WorkCompleted, entity.UpcomingWorks,
            entity.CreatedByEmail, entity.CreatedAt, selectedUpdateIds);
}
