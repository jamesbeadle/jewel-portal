using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jewel.JPMS.Api.Data.Entities;

// The unified Variation Order document (see Jewel.JPMS.Models.VariationOrder) — one row per
// variation from first pricing (Quoting) through Issued to Approved/Rejected.
//
// Mapped onto the historic VariationOrderQuotes table: the quote row always was the document.
// Approval used to mint a second row in a separate VariationOrders table; the
// 20260723120000_UnifyVariationOrders migration folded that table into this one. The table and
// key column keep their historic spelling — persisted identifiers survive renames, the same rule
// as RecordType.Scheduling / the JPMS/SCH- mail tag (see CLAUDE.md terminology).
[Table("VariationOrderQuotes")]
public sealed class VariationOrderEntity
{
    [Key, MaxLength(64)]
    [Column("VariationOrderQuoteId")]
    public string VariationOrderId { get; set; } = "";
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

    // Contract-stage data, written at approval: the minted V-ref ("V18"), the agreed value and
    // the cost-centre the value is committed against. Null / 0 while the order is still quoting.
    [MaxLength(16)]      public string? VariationRef { get; set; }
    public decimal Value { get; set; }
    [MaxLength(32)]      public string? CostCode { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    [MaxLength(256)]     public string? ApprovedByEmail { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
}

// A subcontractor-raised variation request (see Jewel.JPMS.Models.SubcontractorVariationRequest).
// Raised from the portal against one of the sub's own work orders; on acceptance it creates a
// variation order in Quoting (VariationOrderId is then set) which runs the normal lifecycle.
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
    [MaxLength(64)]
    [Column("VariationOrderQuoteId")]
    public string? VariationOrderId { get; set; }
}
