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
    /// <summary>nvarchar(max) — a day's WhatsApp site notes are kept verbatim (2026-09-16).</summary>
    public string Description { get; set; } = "";
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
    /// <summary>SHA-256 (lower-case hex) of the file as received — what "the same image posted
    /// twice" is judged by. Empty on photos stored before 2026-09-16.</summary>
    [MaxLength(64)]      public string ContentHash { get; set; } = "";
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

/// <summary>
/// The weekly Contractor's Report (the FD's spec of 2026-09-15, change 4): one row per report
/// holding what a person enters — the header fields, Look Ahead, Neighbours, H&amp;S, the
/// Building Control liaison line, the subcontractors' attendance and the chosen updates. Every
/// other section is read from the register at build time and never stored. The three JSON
/// columns hold small typed lists (ContractorsReportJson), the way a variation's staged lines do.
/// </summary>
public sealed class ContractorsReportEntity
{
    [Key, MaxLength(64)] public string ContractorsReportId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int Number { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    [MaxLength(32)]      public string ValuationNumber { get; set; } = "";
    [MaxLength(256)]     public string ProgrammeReference { get; set; } = "";
    [MaxLength(256)]     public string PreparedByName { get; set; } = "";
    [MaxLength(256)]     public string IssuedTo { get; set; } = "";
    public DateOnly DateOfIssue { get; set; }
    public string LookAheadJson { get; set; } = "[]";
    [MaxLength(4000)]    public string Neighbours { get; set; } = "";
    [MaxLength(4000)]    public string HealthAndSafety { get; set; } = "";
    [MaxLength(2000)]    public string BuildingControlLiaison { get; set; } = "";
    public string AttendanceJson { get; set; } = "[]";
    public string SelectedUpdateIdsJson { get; set; } = "[]";
    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
