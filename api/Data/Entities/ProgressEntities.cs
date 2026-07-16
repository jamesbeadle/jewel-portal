using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// A site manager's record of progress on the works: a group of photos with a description
/// (e.g. "First-fix carpentry complete to first floor"). Updates are the raw material from
/// which client-facing progress reports are assembled.
/// </summary>
public sealed class ProgressUpdateEntity
{
    [Key, MaxLength(64)] public string ProgressUpdateId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(4096)]    public string Description { get; set; } = "";
    /// <summary>The date the photographed works were carried out (not the upload date).</summary>
    public DateTimeOffset? WorkDate { get; set; }

    // Weather conditions on site, entered manually by the site manager (all optional). Units
    // follow the client-facing report convention: temperatures in °C, wind in mph, precipitation
    // in inches. A blank summary with all-null figures means "no weather recorded".
    [MaxLength(256)]     public string WeatherSummary { get; set; } = "";
    /// <summary>When the conditions were observed (e.g. "Fri, 20 Mar 2026, 10:29").</summary>
    public DateTimeOffset? WeatherObservedAt { get; set; }
    public int? WeatherTempHighC { get; set; }
    public int? WeatherTempLowC { get; set; }
    public int? WeatherWindMph { get; set; }
    public int? WeatherHumidityPercent { get; set; }
    public decimal? WeatherPrecipInches { get; set; }

    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ProgressPhotoEntity
{
    [Key, MaxLength(64)] public string ProgressPhotoId { get; set; } = "";
    [MaxLength(64)]      public string ProgressUpdateId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(512)]     public string FileName { get; set; } = "";
    [MaxLength(1024)]    public string BlobRef { get; set; } = "";
    [MaxLength(256)]     public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public int SortOrder { get; set; }
    [MaxLength(256)]     public string UploadedByEmail { get; set; } = "";
    public DateTimeOffset UploadedAt { get; set; }
}

/// <summary>
/// A client-facing progress report: narrative sections plus a selection of progress updates
/// whose photos illustrate the completed works. The PDF is rendered from the register on every
/// download, so it always reflects the report (and its selected updates) as they stand.
/// </summary>
public sealed class ProgressReportEntity
{
    [Key, MaxLength(64)] public string ProgressReportId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    public DateTimeOffset? PeriodStart { get; set; }
    public DateTimeOffset? PeriodEnd { get; set; }
    [MaxLength(4096)]    public string Introduction { get; set; } = "";
    [MaxLength(4096)]    public string WorkCompleted { get; set; } = "";
    [MaxLength(4096)]    public string UpcomingWorks { get; set; } = "";
    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Selects a progress update onto a report, in presentation order.</summary>
public sealed class ProgressReportSelectionEntity
{
    [Key, MaxLength(64)] public string ProgressReportSelectionId { get; set; } = "";
    [MaxLength(64)]      public string ProgressReportId { get; set; } = "";
    [MaxLength(64)]      public string ProgressUpdateId { get; set; } = "";
    public int SortOrder { get; set; }
}
