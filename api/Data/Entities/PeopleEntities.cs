using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class SubcontractorEntity
{
    [Key, MaxLength(64)] public string SubcontractorId { get; set; } = "";
    [MaxLength(256)]     public string CompanyName { get; set; } = "";
    [MaxLength(256)]     public string ContactName { get; set; } = "";
    [MaxLength(256)]     public string ContactEmail { get; set; } = "";
    [MaxLength(64)]      public string ContactPhone { get; set; } = "";
    [MaxLength(32)]      public string CisStatus { get; set; } = "";
    public DateTimeOffset OnboardedAt { get; set; }

    // Company-directory fields. Category drives filtering (0 = Subcontractor by default). The rest
    // mirror the master-sheet columns.
    public int Category { get; set; }
    [MaxLength(64)]      public string MobileNumber { get; set; } = "";
    [MaxLength(128)]     public string Town { get; set; } = "";
    [MaxLength(128)]     public string County { get; set; } = "";
    [MaxLength(512)]     public string Website { get; set; } = "";
    [MaxLength(128)]     public string Pli { get; set; } = "";
    [MaxLength(64)]      public string PliExpiry { get; set; } = "";
}

// The curated master list of trades. Directory records link to these via SubcontractorTrades, so a
// trade is added deliberately once and reused everywhere — never typed free-text per record (the old
// PrimaryTrade string allowed slash-separated compounds like "Boarding/drylining/Plastering").
public sealed class TradeEntity
{
    [Key, MaxLength(64)] public string TradeId { get; set; } = "";
    [MaxLength(64)]      public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

// Link table: one row per (subcontractor, trade). A directory record can carry several trades.
public sealed class SubcontractorTradeEntity
{
    [Key, MaxLength(64)] public string SubcontractorTradeId { get; set; } = "";
    [MaxLength(64)]      public string SubcontractorId { get; set; } = "";
    [MaxLength(64)]      public string TradeId { get; set; } = "";
}

public sealed class ComplianceDocumentEntity
{
    [Key, MaxLength(64)] public string ComplianceDocumentId { get; set; } = "";
    [MaxLength(64)]      public string SubcontractorId { get; set; } = "";
    [MaxLength(128)]     public string Kind { get; set; } = "";
    [MaxLength(256)]     public string FileName { get; set; } = "";
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}

public sealed class HsRecordEntity
{
    [Key, MaxLength(64)] public string HsRecordId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int Kind { get; set; }
    [MaxLength(512)]     public string Summary { get; set; } = "";
    public int Severity { get; set; }
    public int Status { get; set; }
    [MaxLength(256)]     public string AssignedToEmail { get; set; } = "";
    public DateTimeOffset RaisedAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}

public sealed class HsRecordAttendanceEntity
{
    [Key, MaxLength(64)] public string HsRecordAttendanceId { get; set; } = "";
    [MaxLength(64)]      public string HsRecordId { get; set; } = "";
    [MaxLength(256)]     public string AttendeeName { get; set; } = "";
    [MaxLength(256)]     public string SignatureBlobRef { get; set; } = "";
    public DateTimeOffset SignedAt { get; set; }
}
