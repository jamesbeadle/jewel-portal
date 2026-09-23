using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// A completed public form. AnswersJson holds the answers by question key; a licence check
/// withholds some of them (RedactedAt) and the retention sweep clears them all (DestroyedAt),
/// leaving the row as the record that the destruction happened.
/// </summary>
[Index(nameof(SessionId), Name = "IX_FormSubmissions_SessionId")]
[Index(nameof(SubmittedAt), Name = "IX_FormSubmissions_SubmittedAt")]
public sealed class FormSubmissionEntity
{
    [Key, MaxLength(64)] public string FormSubmissionId { get; set; } = "";
    [MaxLength(64)]      public string FormSlug { get; set; } = "";
    public int Company { get; set; }
    [MaxLength(64)]      public string SessionId { get; set; } = "";
    [MaxLength(64)]      public string? FormInviteId { get; set; }
    [MaxLength(64)]      public string? FormPackId { get; set; }
    [MaxLength(64)]      public string? FormFolderId { get; set; }
    [MaxLength(256)]     public string SubmitterName { get; set; } = "";
    [MaxLength(256)]     public string FilingName { get; set; } = "";
    public bool IsVerifiedLink { get; set; }
    [MaxLength(256)]     public string SentToEmail { get; set; } = "";
    [MaxLength(256)]     public string SentByName { get; set; } = "";
    public string AnswersJson { get; set; } = "{}";
    public int Status { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    [MaxLength(256)]     public string HandledByEmail { get; set; } = "";
    public DateTimeOffset? HandledAt { get; set; }
    public DateTimeOffset? RedactedAt { get; set; }
    public DateTimeOffset? DestroyedAt { get; set; }
    [MaxLength(64)]      public string ClientHash { get; set; } = "";
}

/// <summary>
/// A file sent with a form, posted before the form itself. FormSubmissionId stays empty for a form
/// never sent — the signal an abandoned upload is swept on. A deleted file keeps its row.
/// </summary>
[Index(nameof(SessionId), Name = "IX_FormUploads_SessionId")]
[Index(nameof(FormSubmissionId), Name = "IX_FormUploads_FormSubmissionId")]
public sealed class FormUploadEntity
{
    [Key, MaxLength(64)] public string FormUploadId { get; set; } = "";
    [MaxLength(64)]      public string SessionId { get; set; } = "";
    [MaxLength(64)]      public string FormSlug { get; set; } = "";
    public int Company { get; set; }
    [MaxLength(64)]      public string QuestionKey { get; set; } = "";
    public int Store { get; set; }
    [MaxLength(512)]     public string BlobRef { get; set; } = "";
    [MaxLength(256)]     public string FileName { get; set; } = "";
    [MaxLength(128)]     public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    [MaxLength(64)]      public string? FormSubmissionId { get; set; }
    [MaxLength(64)]      public string ClientHash { get; set; } = "";
    public DateTimeOffset? DeletedAt { get; set; }
    [MaxLength(256)]     public string DeletionReason { get; set; } = "";
}

/// <summary>The person or company everything they send is filed under, and the dates their retention runs from.</summary>
[Index(nameof(NormalizedName), Name = "IX_FormFolders_NormalizedName")]
public sealed class FormFolderEntity
{
    [Key, MaxLength(64)] public string FormFolderId { get; set; } = "";
    [MaxLength(256)]     public string Name { get; set; } = "";
    [MaxLength(256)]     public string NormalizedName { get; set; } = "";
    public int Kind { get; set; }
    public int Company { get; set; }
    public DateOnly? EngagementEndedOn { get; set; }
    public DateOnly? VehicleReturnedOn { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastSubmittedAt { get; set; }
}
