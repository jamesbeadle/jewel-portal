using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class QualificationAssessmentEntity
{
    [Key, MaxLength(64)] public string LeadId { get; set; } = "";
    public int Score { get; set; }
    [MaxLength(4096)]    public string Notes { get; set; } = "";
    [MaxLength(256)]     public string AssessedByEmail { get; set; } = "";
    public DateTimeOffset AssessedAt { get; set; }
}

public sealed class SiteVisitEntity
{
    [Key, MaxLength(64)] public string SiteVisitId { get; set; } = "";
    [MaxLength(64)]      public string LeadId { get; set; } = "";
    public DateTimeOffset ScheduledAt { get; set; }
    [MaxLength(4096)]    public string AttendeeEmailsCsv { get; set; } = "";
    [MaxLength(4096)]    public string Notes { get; set; } = "";
    public int PhotoCount { get; set; }
    public bool IsComplete { get; set; }
}

public sealed class InfoChaseItemEntity
{
    [Key, MaxLength(64)] public string InfoChaseItemId { get; set; } = "";
    [MaxLength(64)]      public string LeadId { get; set; } = "";
    [MaxLength(32)]      public string Kind { get; set; } = "";
    [MaxLength(1024)]    public string Description { get; set; } = "";
    public bool IsReceived { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
}

public sealed class BidDecisionEntity
{
    [Key, MaxLength(64)] public string LeadId { get; set; } = "";
    public bool ShouldBid { get; set; }
    [MaxLength(2048)]    public string Reason { get; set; } = "";
    [MaxLength(256)]     public string DecidedByEmail { get; set; } = "";
    public DateTimeOffset DecidedAt { get; set; }
}

public sealed class ProposalEntity
{
    [Key, MaxLength(64)] public string ProposalId { get; set; } = "";
    [MaxLength(64)]      public string LeadId { get; set; } = "";
    public decimal Value { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    [MaxLength(8192)]    public string NegotiationRoundsJson { get; set; } = "[]";
}

public sealed class LeadOutcomeEntity
{
    [Key, MaxLength(64)] public string LeadId { get; set; } = "";
    public bool IsWon { get; set; }
    [MaxLength(2048)]    public string Reason { get; set; } = "";
    [MaxLength(256)]     public string DecidedByEmail { get; set; } = "";
    public DateTimeOffset DecidedAt { get; set; }
    [MaxLength(64)]      public string? CreatedProjectId { get; set; }
}
