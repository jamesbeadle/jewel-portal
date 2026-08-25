using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// An inbound tender enquiry (see Jewel.JPMS.Models.TenderEnquiry) on a Lead-stage project. Status
/// mirrors TenderEnquiryStatus as an int. Number is a global sequence behind the TEQ-#### tag
/// stem; ReceivedAt is the official date lists lead with, CreatedAt the system stamp.
/// </summary>
public sealed class TenderEnquiryEntity
{
    [Key, MaxLength(64)] public string TenderEnquiryId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int Number { get; set; }
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(256)]     public string ArchitectPracticeName { get; set; } = "";
    [MaxLength(256)]     public string ArchitectContactName { get; set; } = "";
    [MaxLength(256)]     public string ArchitectContactEmail { get; set; } = "";
    [MaxLength(4000)]    public string ScopeSummary { get; set; } = "";
    [MaxLength(256)]     public string ContractForm { get; set; } = "";
    public int Status { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? PqqDueAt { get; set; }
    public DateTimeOffset? TenderDueAt { get; set; }
    public DateTimeOffset? PqqSubmittedAt { get; set; }
    public DateTimeOffset? TenderSubmittedAt { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    [MaxLength(2048)]    public string DecisionNote { get; set; } = "";
    [MaxLength(256)]     public string OwnerEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";
}

/// <summary>One numbered PQQ question and its answer. Replaced wholesale on save (positions
/// re-minted 1..n) — the RequestItems arrangement.</summary>
public sealed class TenderEnquiryAnswerEntity
{
    [Key, MaxLength(64)] public string TenderEnquiryAnswerId { get; set; } = "";
    [MaxLength(64)]      public string TenderEnquiryId { get; set; } = "";
    public int Position { get; set; }
    [MaxLength(2048)]    public string Question { get; set; } = "";
    [MaxLength(8000)]    public string Answer { get; set; } = "";
}

/// <summary>A file kept on a tender enquiry — the questionnaire, the drawings, supporting
/// material. Source mirrors TenderEnquiryAttachmentSource (0 upload, 1 copied off the email).</summary>
public sealed class TenderEnquiryAttachmentEntity
{
    [Key, MaxLength(64)] public string TenderEnquiryAttachmentId { get; set; } = "";
    [MaxLength(64)]      public string TenderEnquiryId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string FileName { get; set; } = "";
    [MaxLength(128)]     public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    [MaxLength(1024)]    public string BlobRef { get; set; } = "";
    public int Source { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    [MaxLength(256)]     public string AddedByEmail { get; set; } = "";
}
