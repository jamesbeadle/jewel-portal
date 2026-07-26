using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One thing attached to a request: either a link to a drawing revision in the project register, or
/// an uploaded file (usually a site photo). Drawing links store the code and revision label as well
/// as the ids, so the RFI still reads correctly if the drawing is later deleted from the register.
/// </summary>
public sealed class RequestAttachmentEntity
{
    [Key, MaxLength(64)] public string RequestAttachmentId { get; set; } = "";
    [MaxLength(64)]      public string RequestId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    // Maps to RequestAttachmentKind (0 = Drawing link, 1 = uploaded File).
    public int Kind { get; set; }

    [MaxLength(64)]  public string? DrawingId { get; set; }
    [MaxLength(64)]  public string? DrawingRevisionId { get; set; }
    [MaxLength(64)]  public string? DrawingCode { get; set; }
    [MaxLength(16)]  public string? RevisionLabel { get; set; }

    [MaxLength(256)]  public string? FileName { get; set; }
    [MaxLength(128)]  public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    [MaxLength(1024)] public string? BlobRef { get; set; }

    [MaxLength(512)] public string? Caption { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    [MaxLength(256)] public string AddedByEmail { get; set; } = "";
}
