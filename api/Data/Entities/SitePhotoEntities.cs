using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// The company-wide site photo pool (2026-09-16): a photograph dropped in before anyone has said
/// which project or day it belongs to. The pool is keyed by content — ContentHash is unique — so
/// the same file dropped twice is one row, and the assistant finds a row from a laptop-side
/// SHA-256 of the same file. Filing copies the image onto a progress update and stamps the three
/// FiledTo columns; the pool row stays so the page can show what is still unfiled. A photo the
/// weekly-report run left out of the report is archived (2026-09-24): the Archive columns say
/// why and which project's week it was dumped in, and the row and file are kept.
/// </summary>
public sealed class SitePhotoEntity
{
    [Key, MaxLength(64)] public string SitePhotoId { get; set; } = "";
    [MaxLength(512)]     public string FileName { get; set; } = "";
    [MaxLength(1024)]    public string BlobRef { get; set; } = "";
    [MaxLength(256)]     public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    /// <summary>SHA-256 (lower-case hex) of the file as received — the match key.</summary>
    [MaxLength(64)]      public string ContentHash { get; set; } = "";
    [MaxLength(256)]     public string UploadedByEmail { get; set; } = "";
    public DateTimeOffset UploadedAt { get; set; }
    [MaxLength(64)]      public string? FiledToProjectId { get; set; }
    [MaxLength(64)]      public string? FiledToProgressUpdateId { get; set; }
    [MaxLength(64)]      public string? FiledToProgressPhotoId { get; set; }
    [MaxLength(256)]     public string FiledByEmail { get; set; } = "";
    public DateTimeOffset? FiledAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public SitePhotoArchiveReason? ArchiveReason { get; set; }
    public string ArchiveNote { get; set; } = "";
    [MaxLength(64)]      public string? ArchivedForProjectId { get; set; }
    public DateOnly? ArchivedForPeriodEnd { get; set; }
    [MaxLength(256)]     public string ArchivedByEmail { get; set; } = "";
}
