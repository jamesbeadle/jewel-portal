using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// A lead (see Jewel.JPMS.Models.Lead) — rebuilt 2026-09-06 for the Sales section: the May 2026
// prototype's columns survive (SiteAddress is the property address; Stage/Source ints were
// remapped by AddSalesStrategies), the rest were added then. The satellite CRM tables of that
// prototype (QualificationAssessments, SiteVisits, InfoChaseItems, BidDecisions, Proposals,
// LeadOutcomes — CrmEntities.cs) are no longer written or read; they stay in the database.
public sealed class LeadEntity
{
    [Key, MaxLength(64)] public string LeadId { get; set; } = "";
    // Legacy free-text reference from the prototype; the LD-#### reference is computed from Number.
    [MaxLength(64)]      public string Reference { get; set; } = "";
    [MaxLength(256)]     public string ContactName { get; set; } = "";
    [MaxLength(256)]     public string ContactEmail { get; set; } = "";
    [MaxLength(64)]      public string ContactPhone { get; set; } = "";
    [MaxLength(256)]     public string CompanyName { get; set; } = "";
    [MaxLength(512)]     public string SiteAddress { get; set; } = "";
    public decimal? EstimatedValue { get; set; }
    public int Source { get; set; }
    public int Stage { get; set; }
    [MaxLength(256)]     public string OwnerEmail { get; set; } = "";
    public DateTimeOffset CapturedAt { get; set; }

    // ---- Sales rebuild (2026-09-06) ----
    // Sequential, human-readable number (rendered as LD-0001) — global, minted by CaptureLeadHandler.
    public int Number { get; set; }
    public int ProspectKind { get; set; }
    [MaxLength(16)]      public string Postcode { get; set; } = "";
    [MaxLength(512)]     public string Summary { get; set; } = "";
    [MaxLength(4000)]    public string Notes { get; set; } = "";
    // The strategy that found the lead; null for inbound / referral / manual. No FK — the
    // handlers own the relationship (a retired strategy keeps its leads).
    [MaxLength(64)]      public string? StrategyId { get; set; }
    public DateTimeOffset StageChangedAt { get; set; }
    // Set by WinLead: the client account and project shell the lead became.
    [MaxLength(64)]      public string? ClientId { get; set; }
    [MaxLength(64)]      public string? ProjectId { get; set; }
    // Set by MoveLeadStage → Lost; cleared on reopen.
    [MaxLength(1024)]    public string? LostReason { get; set; }

    // ---- Imagine (2026-09-06, AddImagine) ----
    // The token behind the lead's private /imagine/{token} page (printed as its QR code). Stored
    // raw, not hashed, on purpose: the lead page must render the QR code again whenever a letter
    // is printed. It opens a design page, not an account. Indexed unique; null until issued.
    [MaxLength(64)]      public string? ImagineToken { get; set; }
    public DateTimeOffset? ImagineTokenIssuedAt { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string DisplayReference => Number > 0
        ? $"LD-{Number:0000}"
        : (string.IsNullOrWhiteSpace(Reference) ? $"LD-{LeadId.PadRight(8, '0')[..8].ToUpperInvariant()}" : Reference);
}

public sealed class BoqLineItemEntity
{
    [Key, MaxLength(64)] public string BoqLineItemId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(512)]     public string Description { get; set; } = "";
    [MaxLength(32)]      public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal RateValue { get; set; }
    [MaxLength(32)]      public string CostCode { get; set; } = "";
    public int Discipline { get; set; }
}

public sealed class RateEntity
{
    [Key, MaxLength(64)] public string RateId { get; set; } = "";
    [MaxLength(64)]      public string Trade { get; set; } = "";
    [MaxLength(256)]     public string Description { get; set; } = "";
    [MaxLength(16)]      public string Unit { get; set; } = "";
    public decimal Value { get; set; }
    [MaxLength(256)]     public string SupplierName { get; set; } = "";
    public DateTimeOffset LastPricedAt { get; set; }
}

public sealed class DrawingEntity
{
    [Key, MaxLength(64)] public string DrawingId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    // Code and title are optional — blank means "not given yet"; the register then names the
    // drawing by its latest file.
    [MaxLength(64)]      public string DrawingCode { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    // The latest APPROVED revision label; null until a revision has been approved.
    [MaxLength(16)]      public string? CurrentApprovedRevisionLabel { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    // The register folder this drawing sits in; null = ungrouped. No FK — deleting a folder
    // nulls this out (DeleteDrawingFolderHandler) rather than cascading into the drawings.
    [MaxLength(64)]      public string? DrawingFolderId { get; set; }
}

/// <summary>
/// A named group on a project's drawing register. Folders nest through
/// <see cref="ParentDrawingFolderId"/> (null = top level). Drawings point at a folder via
/// <see cref="DrawingEntity.DrawingFolderId"/>; deleting a folder moves its drawings and
/// sub-folders up one level. No FK on the parent, for the same reason as DrawingFolderId.
/// </summary>
public sealed class DrawingFolderEntity
{
    [Key, MaxLength(64)] public string DrawingFolderId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(128)]     public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    [MaxLength(64)]      public string? ParentDrawingFolderId { get; set; }
}

public sealed class DrawingRevisionEntity
{
    [Key, MaxLength(64)] public string DrawingRevisionId { get; set; } = "";
    [MaxLength(64)]      public string DrawingId { get; set; } = "";
    // Blank = no revision given; settable later via SetDrawingRevisionLabel.
    [MaxLength(16)]      public string RevisionLabel { get; set; } = "";
    [MaxLength(256)]     public string FileName { get; set; } = "";
    // Blank = issuer not recorded.
    [MaxLength(256)]     public string IssuedByEmail { get; set; } = "";
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? SupersededAt { get; set; }
    public bool IsAmbiguous { get; set; }
    public int ViewCount { get; set; }

    // Approval workflow + stored file.
    public int ApprovalStatus { get; set; }            // maps to DrawingApprovalStatus (0=Unapproved,1=Approved,2=Archived)
    [MaxLength(1024)] public string? BlobRef { get; set; }
    [MaxLength(128)]  public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    [MaxLength(256)]  public string? ApprovedByEmail { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }

    // Drawing pipeline status (whiteboard workflow: Bluebeam extracts metadata/structural file
    // data into the portal, then Claude analyses changes and triggers workflows). Null = that
    // stage hasn't run for this revision yet; the pipeline stamps these when each stage lands.
    public DateTimeOffset? MetadataExtractedAt { get; set; }
    public DateTimeOffset? AnalysedAt { get; set; }
}
