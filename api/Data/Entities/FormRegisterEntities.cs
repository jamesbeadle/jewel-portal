using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>The right-to-work register: the checker's record, kept in the restricted store's terms.</summary>
public sealed class RightToWorkCheckEntity
{
    [Key, MaxLength(64)] public string RightToWorkCheckId { get; set; } = "";
    [MaxLength(64)]      public string? FormSubmissionId { get; set; }
    [MaxLength(256)]     public string PersonName { get; set; } = "";
    [MaxLength(256)]     public string Email { get; set; } = "";
    public int Company { get; set; }
    [MaxLength(256)]     public string JobRole { get; set; } = "";
    public int EngagedAs { get; set; }
    public DateOnly? EngagedSince { get; set; }
    public int Route { get; set; }
    [MaxLength(64)]      public string Reference { get; set; } = "";
    [MaxLength(128)]     public string IdspProvider { get; set; } = "";
    public int SeenVia { get; set; }
    [MaxLength(256)]     public string DocumentReference { get; set; } = "";
    [MaxLength(256)]     public string CheckedByName { get; set; } = "";
    public DateOnly CheckedOn { get; set; }
    public bool IsDocumentGenuine { get; set; }
    public bool IsLikenessConfirmed { get; set; }
    public bool IsPermittedToDoTheWork { get; set; }
    public bool IsEvidenceFiled { get; set; }
    public bool IsTimeLimited { get; set; }
    public DateOnly? PermissionExpiresOn { get; set; }
    public DateOnly? FollowUpOn { get; set; }
    public int Outcome { get; set; }
    [MaxLength(2000)]    public string Notes { get; set; } = "";
    [MaxLength(64)]      public string? EvidenceUploadId { get; set; }
    [MaxLength(256)]     public string RecordedByEmail { get; set; } = "";
    public DateTimeOffset RecordedAt { get; set; }
    public DateOnly? EngagementEndedOn { get; set; }
    public DateOnly? ConfirmedOn { get; set; }
}

/// <summary>A ticket or certificate on the training register; LastChasedAt is when its renewal was last asked for.</summary>
public sealed class TrainingRecordEntity
{
    [Key, MaxLength(64)] public string TrainingRecordId { get; set; } = "";
    [MaxLength(64)]      public string? FormSubmissionId { get; set; }
    public int Company { get; set; }
    [MaxLength(256)]     public string PersonName { get; set; } = "";
    [MaxLength(256)]     public string Email { get; set; } = "";
    [MaxLength(256)]     public string Course { get; set; } = "";
    [MaxLength(256)]     public string Provider { get; set; } = "";
    [MaxLength(128)]     public string CertificateNumber { get; set; } = "";
    public DateOnly CompletedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    [MaxLength(64)]      public string? CertificateUploadId { get; set; }
    [MaxLength(256)]     public string AcceptedByEmail { get; set; } = "";
    public DateTimeOffset AcceptedAt { get; set; }
    public DateTimeOffset? LastChasedAt { get; set; }
    public int ChaseCount { get; set; }
    public DateOnly? EndedOn { get; set; }
}

/// <summary>One NO from a workstation assessment, open until it is fixed or accepted.</summary>
[Index(nameof(FormSubmissionId), Name = "IX_WorkstationActions_FormSubmissionId")]
public sealed class WorkstationActionEntity
{
    [Key, MaxLength(64)] public string WorkstationActionId { get; set; } = "";
    [MaxLength(64)]      public string FormSubmissionId { get; set; } = "";
    [MaxLength(256)]     public string PersonName { get; set; } = "";
    [MaxLength(128)]     public string Workstation { get; set; } = "";
    [MaxLength(64)]      public string QuestionKey { get; set; } = "";
    [MaxLength(1024)]    public string Action { get; set; } = "";
    public int State { get; set; }
    [MaxLength(1000)]    public string Note { get; set; } = "";
    [MaxLength(256)]     public string ResolvedByEmail { get; set; } = "";
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset RaisedAt { get; set; }
}

/// <summary>The office's check of a vehicle form: only the outcome is kept once it is recorded.</summary>
[Index(nameof(FormSubmissionId), IsUnique = true, Name = "IX_DrivingLicenceChecks_FormSubmissionId")]
public sealed class DrivingLicenceCheckEntity
{
    [Key, MaxLength(64)] public string DrivingLicenceCheckId { get; set; } = "";
    [MaxLength(64)]      public string FormSubmissionId { get; set; } = "";
    public DateOnly DvlaCheckedOn { get; set; }
    public bool IsWithinInsuranceCriteria { get; set; }
    [MaxLength(1000)]    public string Note { get; set; } = "";
    [MaxLength(256)]     public string CheckedByEmail { get; set; } = "";
    public DateTimeOffset CheckedAt { get; set; }
    public int PhotosDeleted { get; set; }
}
