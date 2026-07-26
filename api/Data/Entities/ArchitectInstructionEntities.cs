using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One Architect's Instruction on a project. The file itself lives in blob storage (private
/// container, proxied downloads) — only the reference is stored here, exactly as drawing revisions
/// and compliance documents do it.
/// </summary>
public sealed class ArchitectInstructionEntity
{
    [Key, MaxLength(64)] public string ArchitectInstructionId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    // JPMS's own per-project sequence, rendered AI-0001. Number is what the sequence advances on.
    public int Number { get; set; }
    [MaxLength(64)]      public string Reference { get; set; } = "";
    // The architect's own number as written on the document. Free text — practices differ.
    [MaxLength(128)]     public string InstructionRef { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(2048)]    public string? Notes { get; set; }
    public DateTimeOffset? InstructedAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    [MaxLength(256)]     public string IssuedByEmail { get; set; } = "";
    [MaxLength(256)]     public string FiledByEmail { get; set; } = "";
    // Maps to ArchitectInstructionSource (0 = Upload, 1 = Email).
    public int Source { get; set; }

    // The stored document. Null throughout on a placeholder row filed before the PDF arrived.
    [MaxLength(256)]  public string? FileName { get; set; }
    [MaxLength(128)]  public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    [MaxLength(1024)] public string? BlobRef { get; set; }
}

/// <summary>
/// Join row: which variations an instruction covers. Many-to-many on purpose — one instruction
/// routinely instructs several variations, and a variation can be justified by more than one.
/// Follows the house style of loose string ids with no FK constraints.
/// </summary>
public sealed class ArchitectInstructionVariationEntity
{
    [Key, MaxLength(64)] public string ArchitectInstructionVariationId { get; set; } = "";
    [MaxLength(64)]      public string ArchitectInstructionId { get; set; } = "";
    [MaxLength(64)]      public string VariationOrderId { get; set; } = "";
    public DateTimeOffset LinkedAt { get; set; }
    [MaxLength(256)]     public string LinkedByEmail { get; set; } = "";
}
