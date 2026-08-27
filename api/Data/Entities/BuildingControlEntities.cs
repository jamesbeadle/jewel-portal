using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// The project's case with a building control body (regime, their reference, the contact, where
/// the case has got to). String-keyed with no FKs — the to-do/calendar arrangement; the handlers
/// own the cascades. One ACTIVE case per project is a handler rule, not a constraint, so a
/// lapsed case's successor never needs a migration.
/// </summary>
public sealed class BuildingControlCaseEntity
{
    [Key, MaxLength(64)] public string BuildingControlCaseId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";

    // Sequential, human-readable case number (rendered as BC-0001). Global — like defect and
    // to-do numbers — so the tag stem is unique across the flat JPMS mailbox-category space.
    public int Number { get; set; }

    public int Regime { get; set; }
    [MaxLength(256)]     public string BodyName { get; set; } = "";
    [MaxLength(128)]     public string BodyReference { get; set; } = "";
    [MaxLength(256)]     public string ContactName { get; set; } = "";
    [MaxLength(256)]     public string ContactEmail { get; set; } = "";
    [MaxLength(64)]      public string ContactPhone { get; set; } = "";
    public int Status { get; set; }
    public DateTimeOffset? NoticeSubmittedOn { get; set; }
    public DateTimeOffset? AcceptedOn { get; set; }
    public DateTimeOffset? CompletionCertifiedOn { get; set; }
    [MaxLength(4096)]    public string Notes { get; set; } = "";
    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    // The canonical reference this case's emails are tagged with ("BC-0001" -> "JPMS/BC-0001").
    // Computed, not stored; the id-derived fallback keeps two unnumbered rows from ever sharing
    // a stem (the DefectEntity rule).
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Reference => Number > 0
        ? $"BC-{Number:0000}"
        : $"BC-{BuildingControlCaseId.PadRight(8, '0')[..8].ToUpperInvariant()}";
}

/// <summary>One inspection stage on a case — the register row the Building Control tab is built
/// around. DisplayOrder is the running order of stages; Number is the global BCI sequence that
/// mints the tag stem.</summary>
public sealed class BuildingControlInspectionEntity
{
    [Key, MaxLength(64)] public string BuildingControlInspectionId { get; set; } = "";
    [MaxLength(64)]      public string BuildingControlCaseId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";

    // Sequential, human-readable inspection number (rendered as BCI-0001). Global, for the same
    // flat-tag-space reason as the case number.
    public int Number { get; set; }

    [MaxLength(256)]     public string StageName { get; set; } = "";
    public int Status { get; set; }
    public DateTimeOffset? BookedFor { get; set; }
    public DateTimeOffset? InspectedAt { get; set; }
    [MaxLength(2048)]    public string OutcomeNotes { get; set; } = "";
    [MaxLength(256)]     public string InspectorName { get; set; } = "";
    public int DisplayOrder { get; set; }
    [MaxLength(256)]     public string RaisedByEmail { get; set; } = "";
    public DateTimeOffset RaisedAt { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Reference => Number > 0
        ? $"BCI-{Number:0000}"
        : $"BCI-{BuildingControlInspectionId.PadRight(8, '0')[..8].ToUpperInvariant()}";
}

/// <summary>
/// A file kept on the case or on one inspection — exactly one parent id is set. Bytes live in
/// the private building-control container (BlobRef); the row is the register, and downloads are
/// proxied — the tender-enquiry attachment arrangement.
/// </summary>
public sealed class BuildingControlAttachmentEntity
{
    [Key, MaxLength(64)] public string BuildingControlAttachmentId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string? BuildingControlCaseId { get; set; }
    [MaxLength(64)]      public string? BuildingControlInspectionId { get; set; }
    public int Kind { get; set; }
    [MaxLength(512)]     public string FileName { get; set; } = "";
    [MaxLength(256)]     public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    [MaxLength(1024)]    public string BlobRef { get; set; } = "";
    public int Source { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    [MaxLength(256)]     public string AddedByEmail { get; set; } = "";
}
