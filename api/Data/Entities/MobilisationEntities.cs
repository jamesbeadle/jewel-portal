using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class MobilisationItemEntity
{
    [Key, MaxLength(64)] public string MobilisationItemId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(512)]     public string Description { get; set; } = "";
    [MaxLength(256)]     public string OwnerEmail { get; set; } = "";
    public bool IsComplete { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class WalkRoundNoteEntity
{
    [Key, MaxLength(64)] public string WalkRoundNoteId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string AuthorEmail { get; set; } = "";
    [MaxLength(4096)]    public string Notes { get; set; } = "";
    public int PhotoCount { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
}

public sealed class DrawingIssueRecordEntity
{
    [Key, MaxLength(64)] public string DrawingIssueRecordId { get; set; } = "";
    [MaxLength(64)]      public string DrawingRevisionId { get; set; } = "";
    [MaxLength(64)]      public string Source { get; set; } = "";
    [MaxLength(256)]     public string IssuedByName { get; set; } = "";
    public DateTimeOffset IssuedAt { get; set; }
    [MaxLength(2048)]    public string Notes { get; set; } = "";
}
