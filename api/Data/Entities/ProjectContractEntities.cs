using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One row per project — the contract it is let under. Enums persist as int, ids are compact GUID
/// strings, no FK relationships (by-id only), matching every other JPMS table.
///
/// <para>Uniqueness on <see cref="ProjectId"/> is enforced by an index created in the migration, not
/// by a navigation property. The handlers treat it as an upsert.</para>
/// </summary>
public sealed class ProjectContractEntity
{
    [Key, MaxLength(64)] public string ProjectContractId { get; set; } = "";
    [MaxLength(64)] public string ProjectId { get; set; } = "";

    // ---- The form ----
    public int Form { get; set; }
    [MaxLength(16)] public string? FormEdition { get; set; }
    [MaxLength(4000)] public string? BespokeDeviations { get; set; }

    // ---- The parties ----
    [MaxLength(256)] public string? EmployerName { get; set; }
    [MaxLength(256)] public string? ContractAdministratorName { get; set; }
    [MaxLength(256)] public string? ContractAdministratorEmail { get; set; }
    [MaxLength(256)] public string? ArchitectName { get; set; }
    [MaxLength(256)] public string? ArchitectEmail { get; set; }
    [MaxLength(256)] public string? ContractorName { get; set; }

    // ---- The money ----
    public decimal ContractSum { get; set; }
    public decimal LiquidatedDamagesPerWeek { get; set; }

    // ---- The dates ----
    public DateTimeOffset? ContractDate { get; set; }
    public DateTimeOffset? PossessionDate { get; set; }
    public DateTimeOffset? CompletionDate { get; set; }

    // ---- Retention and defects ----
    public decimal RetentionPercent { get; set; }
    public decimal RetentionPercentAfterCompletion { get; set; }
    public int DefectsLiabilityPeriodMonths { get; set; }

    // ---- The payment mechanism ----
    public int? ApplicationCutOffDayOfMonth { get; set; }
    public int PaymentNoticeDays { get; set; }
    public int PayLessNoticeDays { get; set; }
    public int FinalDateForPaymentDays { get; set; }

    // ---- Overheads, profit and attendance ----
    public decimal OhpDirectWorksPercent { get; set; }
    public decimal OhpSubcontractorPercent { get; set; }
    public decimal AttendanceOnClientDirectPercent { get; set; }
    public decimal DayworkLabourPercent { get; set; }
    public decimal DayworkMaterialsPercent { get; set; }
    public decimal DayworkPlantPercent { get; set; }

    // ---- The executed document. Nullable throughout: terms may be entered before the PDF lands,
    //      and the PDF may land before the terms are keyed. Naming follows DrawingRevisionEntity
    //      (BlobRef / ContentType / FileSizeBytes), not ComplianceDocumentEntity.
    [MaxLength(1024)] public string? DocumentBlobRef { get; set; }
    [MaxLength(256)] public string? DocumentFileName { get; set; }
    [MaxLength(128)] public string? DocumentContentType { get; set; }
    public long? DocumentFileSizeBytes { get; set; }
    public DateTimeOffset? DocumentUploadedAt { get; set; }
    [MaxLength(256)] public string? DocumentUploadedByEmail { get; set; }

    [MaxLength(256)] public string? UpdatedByEmail { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// One row per contract amendment — a deed of variation, side letter or supplemental agreement,
/// each with its own stored document. Amendments accumulate in date order; they are never a
/// version chain on the executed contract (see AttachProjectContractDocumentHandler — replacing
/// that document means the wrong file was uploaded, whereas an amendment is a real event).
///
/// <para>Keyed to the project, not to ProjectContracts — an amendment can be filed before the
/// terms have been keyed, exactly as the executed document can. The document columns are NOT NULL:
/// a row only exists once its file has been stored, so there is never a placeholder amendment.</para>
/// </summary>
public sealed class ProjectContractAmendmentEntity
{
    [Key, MaxLength(64)] public string ProjectContractAmendmentId { get; set; } = "";
    [MaxLength(64)] public string ProjectId { get; set; } = "";

    // ---- What it is ----
    [MaxLength(256)] public string Title { get; set; } = "";
    public DateTimeOffset? AmendmentDate { get; set; }
    [MaxLength(4000)] public string? Notes { get; set; }

    // ---- The document. Naming follows ProjectContractEntity's document block. ----
    [MaxLength(1024)] public string DocumentBlobRef { get; set; } = "";
    [MaxLength(256)] public string DocumentFileName { get; set; } = "";
    [MaxLength(128)] public string DocumentContentType { get; set; } = "";
    public long DocumentFileSizeBytes { get; set; }
    public DateTimeOffset DocumentUploadedAt { get; set; }
    [MaxLength(256)] public string DocumentUploadedByEmail { get; set; } = "";

    [MaxLength(256)] public string? UpdatedByEmail { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
