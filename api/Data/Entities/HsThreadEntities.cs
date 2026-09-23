using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// The thread on an H&amp;S record (2026-09-23, Katy-Louise's asks of 15 Sep): what the site manager
/// and the officer say on a corrective action, the photographs sent with it, and the events the
/// digest emails are built from. No FKs, as everywhere else; the handlers own the cascades. The
/// thread reads per record; the digest sweep reads the events not yet sent — the indexes say so.
/// </summary>
[Index(nameof(HsRecordId), Name = "IX_HsRecordComments_HsRecordId")]
public sealed class HsRecordCommentEntity
{
    [Key, MaxLength(64)] public string HsRecordCommentId { get; set; } = "";
    [MaxLength(64)]      public string HsRecordId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string AuthorEmail { get; set; } = "";
    [MaxLength(256)]     public string AuthorName { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTimeOffset PostedAt { get; set; }
}

/// <summary>A photograph on a comment, stored through the progress photo store under the record's own key.</summary>
[Index(nameof(HsRecordId), Name = "IX_HsRecordPhotos_HsRecordId")]
public sealed class HsRecordPhotoEntity
{
    [Key, MaxLength(64)] public string HsRecordPhotoId { get; set; } = "";
    [MaxLength(64)]      public string HsRecordId { get; set; } = "";
    [MaxLength(64)]      public string HsRecordCommentId { get; set; } = "";
    [MaxLength(512)]     public string FileName { get; set; } = "";
    [MaxLength(1024)]    public string BlobRef { get; set; } = "";
    [MaxLength(256)]     public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    [MaxLength(64)]      public string ContentHash { get; set; } = "";
    public DateTimeOffset UploadedAt { get; set; }
}

/// <summary>
/// One thing that happened on a record — a comment, a status change, an action minted by an
/// audit's Issue — waiting to be told to the other side. The ten-minute sweep gathers a
/// project's unsent events once its last one is old enough to call the sitting over, sends ONE
/// email per recipient, and stamps NotifiedAt; a row never goes twice.
/// </summary>
[Index(nameof(NotifiedAt), Name = "IX_HsRecordEvents_NotifiedAt")]
public sealed class HsRecordEventEntity
{
    [Key, MaxLength(64)] public string HsRecordEventId { get; set; } = "";
    [MaxLength(64)]      public string HsRecordId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int Kind { get; set; }
    [MaxLength(1024)]    public string Detail { get; set; } = "";
    [MaxLength(256)]     public string ByEmail { get; set; } = "";
    [MaxLength(256)]     public string ByName { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? NotifiedAt { get; set; }
}
