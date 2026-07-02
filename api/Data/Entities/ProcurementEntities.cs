using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class BidPackageEntity
{
    [Key, MaxLength(64)] public string BidPackageId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(64)]      public string Trade { get; set; } = "";
    public int Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    [MaxLength(256)]     public string OwnerEmail { get; set; } = "";

    // Parent Variation Order Quote, when this package belongs to one. Null for standalone packages.
    [MaxLength(64)]      public string? VariationOrderQuoteId { get; set; }

    // Sequential, human-readable package number (rendered BPI-0001). The BPI reference is the stem
    // tagged on the package's emails so RFT responses group under it in the Bid Package Invites section.
    public int Number { get; set; }

    // Canonical reference this package's emails are tagged with ("JPMS/BPI-0001"). Falls back to an
    // id-derived stem for legacy rows that predate numbering. Computed, not stored.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Reference => Number > 0
        ? $"BPI-{Number:0000}"
        : "BPI-" + (BidPackageId.Length >= 8 ? BidPackageId[..8] : BidPackageId).ToUpperInvariant();
}

// A subcontractor invited to tender for a bid package. One row per (package, subcontractor).
public sealed class BidPackageRecipientEntity
{
    [Key, MaxLength(64)] public string RecipientId { get; set; } = "";
    [MaxLength(64)]      public string BidPackageId { get; set; } = "";
    [MaxLength(64)]      public string SubcontractorId { get; set; } = "";
    public int Status { get; set; }
    public DateTimeOffset InvitedAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
}

// A scope line on a bid package, grouped by Trade/speciality. Pricing is captured per response.
public sealed class BidPackageLineItemEntity
{
    [Key, MaxLength(64)] public string LineItemId { get; set; } = "";
    [MaxLength(64)]      public string BidPackageId { get; set; } = "";
    [MaxLength(512)]     public string Description { get; set; } = "";
    [MaxLength(32)]      public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    [MaxLength(64)]      public string Trade { get; set; } = "";
    public int SortOrder { get; set; }

    // Commercial home of this line — 0 Unassigned, 1 ContractLine, 2 Variation (BidPackageLineCoverage).
    // Exactly one of BoqLineItemId / VariationOrderQuoteId is set to match, enforced by the handler.
    public int Coverage { get; set; }
    [MaxLength(64)]      public string? BoqLineItemId { get; set; }
    [MaxLength(64)]      public string? VariationOrderQuoteId { get; set; }
}

public sealed class QuoteEntity
{
    [Key, MaxLength(64)] public string QuoteId { get; set; } = "";
    [MaxLength(64)]      public string BidPackageId { get; set; } = "";
    [MaxLength(64)]      public string SubcontractorId { get; set; } = "";
    public decimal Value { get; set; }
    [MaxLength(1024)]    public string Notes { get; set; } = "";
    public DateTimeOffset ReceivedAt { get; set; }
    public bool IsDeclined { get; set; }
}

public sealed class WorkOrderEntity
{
    [Key, MaxLength(64)] public string WorkOrderId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string BidPackageId { get; set; } = "";
    [MaxLength(64)]      public string SubcontractorId { get; set; } = "";
    public decimal Value { get; set; }
    [MaxLength(1024)]    public string Scope { get; set; } = "";
    public DateTimeOffset AwardedAt { get; set; }
    [MaxLength(256)]     public string AwardedByEmail { get; set; } = "";
}

public sealed class RequestEntity
{
    [Key, MaxLength(64)] public string RequestId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int Kind { get; set; }
    [MaxLength(64)]      public string Reference { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(2048)]    public string Description { get; set; } = "";
    public int Status { get; set; }
    public decimal? Value { get; set; }
    [MaxLength(256)]     public string RaisedByEmail { get; set; } = "";
    public DateTimeOffset RaisedAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
    [MaxLength(2048)]    public string? ResponseText { get; set; }
    [MaxLength(256)]     public string? RespondedByEmail { get; set; }
    public bool ImpliesVariation { get; set; }
    [MaxLength(256)]     public string? RaisedTo { get; set; }
    [MaxLength(256)]     public string? DrawingRef { get; set; }
    public DateTimeOffset? ResponseDue { get; set; }
    [MaxLength(512)]     public string? RelatedDrawingSpec { get; set; }
    [MaxLength(4000)]    public string? InternalNotes { get; set; }
    [MaxLength(4000)]    public string? ClientNotes { get; set; }

    // Sequential, human-readable request number (rendered as REQ-0001). Used as the name of the
    // request's Outlook folder in the projects@ mailbox so triaged emails can be grouped per request.
    public int Number { get; set; }

    // True once an RFI has spawned an RFQ. Gates creation of a Variation Order Quote (VOQ).
    public bool HasRfq { get; set; }

    // Owning client account. Architect email for RFI promotion resolves from this client first,
    // falling back to the project's Architect contact. Null until the request is linked to a client.
    [MaxLength(64)]     public string? ClientId { get; set; }

    // Graph id of this request's mailbox folder, set the first time an email is filed against it.
    // Cached so subsequent emails for the same request reuse the folder rather than recreating it.
    [MaxLength(450)]     public string? MailboxFolderId { get; set; }

    // The unqualified reference stem for this record's mailbox tag. Prefers the human reference;
    // falls back to REQ-NNNN when blank so a stem is always well-formed. Computed, not stored.
    // NB: the actual mailbox tag is project-qualified (references are only unique per project, tags
    // share one flat category space) — see RequestTags, which turns this stem into e.g.
    // "JBB-2026-001-RFI-001" -> category "JPMS/JBB-2026-001-RFI-001".
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string TagReference =>
        string.IsNullOrWhiteSpace(Reference) ? $"REQ-{Number:0000}" : Reference.Trim();
}
