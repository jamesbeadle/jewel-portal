using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// A Variation Order Quote (see Jewel.JPMS.Models.VariationOrderQuote). Created from an RFQ; owns bid
// packages via BidPackageEntity.VariationOrderQuoteId.
public sealed class VariationOrderQuoteEntity
{
    [Key, MaxLength(64)] public string VariationOrderQuoteId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string RequestId { get; set; } = "";
    public int Number { get; set; }
    [MaxLength(64)]      public string Reference { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(2048)]    public string Description { get; set; } = "";
    public int Status { get; set; }
    [MaxLength(64)]      public string? SelectedBidPackageId { get; set; }
    [MaxLength(64)]      public string? SelectedSubcontractorId { get; set; }
    public decimal? EstimatedValue { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset? ApprovedAt { get; set; }
    [MaxLength(256)]     public string? ApprovedByEmail { get; set; }
}

// A subcontractor-raised variation request (see Jewel.JPMS.Models.SubcontractorVariationRequest).
// Raised from the portal against one of the sub's own work orders; on acceptance it creates a VOQ
// (VariationOrderQuoteId is then set) which runs the normal approval pipeline.
public sealed class SubcontractorVariationRequestEntity
{
    [Key, MaxLength(64)] public string VariationRequestId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string WorkOrderId { get; set; } = "";
    [MaxLength(64)]      public string SubcontractorId { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(2048)]    public string Description { get; set; } = "";
    public decimal ProposedValue { get; set; }
    public int Status { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    [MaxLength(256)]     public string? ReviewedByEmail { get; set; }
    [MaxLength(1024)]    public string RejectionReason { get; set; } = "";
    [MaxLength(64)]      public string? VariationOrderQuoteId { get; set; }
}

// A Variation Order (see Jewel.JPMS.Models.VariationOrder). Raised when a VOQ is approved.
public sealed class VariationOrderEntity
{
    [Key, MaxLength(64)] public string VariationOrderId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string VariationOrderQuoteId { get; set; } = "";
    [MaxLength(64)]      public string RequestId { get; set; } = "";
    public int Number { get; set; }
    [MaxLength(16)]      public string VariationRef { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(2048)]    public string Description { get; set; } = "";
    public int Status { get; set; }
    public decimal Value { get; set; }
    [MaxLength(64)]      public string? SubcontractorId { get; set; }
    [MaxLength(32)]      public string CostCode { get; set; } = "";
    public DateTimeOffset ApprovedAt { get; set; }
    [MaxLength(256)]     public string ApprovedByEmail { get; set; } = "";
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
}
