using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class ValuationLineItemEntity
{
    [Key, MaxLength(64)] public string ValuationLineItemId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int ElementType { get; set; }
    [MaxLength(16)]      public string SectionCode { get; set; } = "";
    [MaxLength(128)]     public string SectionName { get; set; } = "";
    [MaxLength(16)]      public string VariationRef { get; set; } = "";
    [MaxLength(256)]     public string VariationTitle { get; set; } = "";
    public int LineType { get; set; }
    [MaxLength(32)]      public string CostCode { get; set; } = "";
    [MaxLength(512)]     public string Description { get; set; } = "";
    [MaxLength(16)]      public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal LineAmount { get; set; }
    [MaxLength(512)]     public string Comments { get; set; } = "";
    public int DisplayOrder { get; set; }
}

public sealed class ValuationClaimEntity
{
    [Key, MaxLength(64)] public string ValuationClaimId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int ClaimNumber { get; set; }
    public DateTimeOffset ClaimDate { get; set; }
    public int Status { get; set; }
    public decimal RetentionPercent { get; set; }
    public decimal RetentionReleasePercent { get; set; }
    public DateTimeOffset? PreapprovedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public decimal ContractSum { get; set; }
    public decimal NetVariations { get; set; }
    public decimal RevisedContractSum { get; set; }
    public decimal TotalWorksComplete { get; set; }
    public decimal RetentionHeld { get; set; }
    public decimal RetentionReleased { get; set; }
    public decimal CertifiedToDate { get; set; }
    public decimal PaymentDueExVat { get; set; }
}

public sealed class ClaimLineEntity
{
    [Key, MaxLength(64)] public string ClaimLineId { get; set; } = "";
    [MaxLength(64)]      public string ValuationClaimId { get; set; } = "";
    [MaxLength(64)]      public string ValuationLineItemId { get; set; } = "";
    public decimal PercentComplete { get; set; }
    public decimal CumulativeClaimed { get; set; }
    public decimal PeriodIncrement { get; set; }
}

// Immutable line-level copy of the valuation report frozen at a moment in time (invoice
// submission or on-demand period end). Values are copied, never referenced — live edits
// must not disturb what was submitted.
public sealed class ValuationReportSnapshotEntity
{
    [Key, MaxLength(64)] public string ValuationReportSnapshotId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string? ValuationInvoiceId { get; set; }
    [MaxLength(64)]      public string? ValuationClaimId { get; set; }
    [MaxLength(256)]     public string Label { get; set; } = "";
    public DateTimeOffset TakenAt { get; set; }
    public bool IsSuperseded { get; set; }
    public decimal ContractSum { get; set; }
    public decimal NetVariations { get; set; }
    public decimal RevisedContractSum { get; set; }
    public decimal TotalWorksComplete { get; set; }
    public decimal RetentionPercent { get; set; }
    public decimal RetentionHeld { get; set; }
    public decimal RetentionReleasePercent { get; set; }
    public decimal RetentionReleased { get; set; }
    public decimal CertifiedToDate { get; set; }
    public decimal PaymentDueExVat { get; set; }
}

public sealed class ValuationReportSnapshotLineEntity
{
    [Key, MaxLength(64)] public string ValuationReportSnapshotLineId { get; set; } = "";
    [MaxLength(64)]      public string ValuationReportSnapshotId { get; set; } = "";
    [MaxLength(64)]      public string SourceValuationLineItemId { get; set; } = "";
    public int ElementType { get; set; }
    [MaxLength(16)]      public string SectionCode { get; set; } = "";
    [MaxLength(128)]     public string SectionName { get; set; } = "";
    [MaxLength(16)]      public string VariationRef { get; set; } = "";
    [MaxLength(256)]     public string VariationTitle { get; set; } = "";
    public int LineType { get; set; }
    [MaxLength(32)]      public string CostCode { get; set; } = "";
    [MaxLength(512)]     public string Description { get; set; } = "";
    [MaxLength(16)]      public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal LineAmount { get; set; }
    public decimal PercentComplete { get; set; }
    public decimal CumulativeClaimed { get; set; }
    public decimal PeriodIncrement { get; set; }
    [MaxLength(512)]     public string Comments { get; set; } = "";
    public int DisplayOrder { get; set; }
}
