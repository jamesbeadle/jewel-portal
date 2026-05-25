using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class BoqSignOffEntity
{
    [Key, MaxLength(64)] public string BoqSignOffId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string SignedOffByEmail { get; set; } = "";
    public DateTimeOffset SignedOffAt { get; set; }
    public decimal TenderTotalAtSignOff { get; set; }
}

public sealed class PracticalCompletionEntity
{
    [Key, MaxLength(64)] public string PracticalCompletionId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public DateTimeOffset AchievedAt { get; set; }
    [MaxLength(256)]     public string? CertificateBlobRef { get; set; }
    [MaxLength(256)]     public string IssuedByEmail { get; set; } = "";
    public bool IsClientSigned { get; set; }
}

public sealed class HandoverPackItemEntity
{
    [Key, MaxLength(64)] public string HandoverPackItemId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string Label { get; set; } = "";
    [MaxLength(1024)]    public string Detail { get; set; } = "";
    public bool IsReady { get; set; }
    [MaxLength(256)]     public string? EvidenceBlobRef { get; set; }
}

public sealed class SettlementRecordEntity
{
    [Key, MaxLength(64)] public string SettlementRecordId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public decimal FinalContractValue { get; set; }
    public decimal FinalCost { get; set; }
    public decimal FinalMargin { get; set; }
    public DateTimeOffset AgreedAt { get; set; }
    public bool IsClientSigned { get; set; }
}

public sealed class VatAnalysisEntity
{
    [Key, MaxLength(64)] public string VatAnalysisId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public decimal ZeroRatedAmount { get; set; }
    public decimal StandardRatedAmount { get; set; }
    [MaxLength(2048)]    public string Notes { get; set; } = "";
    public bool IsClientConfirmed { get; set; }
    public bool IsArchitectConfirmed { get; set; }
}

public sealed class RetentionReleaseEntity
{
    [Key, MaxLength(64)] public string RetentionReleaseId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTimeOffset ReleasedAt { get; set; }
    public bool IsPublishedDownstream { get; set; }
}

public sealed class PhotoEntity
{
    [Key, MaxLength(64)] public string PhotoId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int AttachedKind { get; set; }
    [MaxLength(64)]      public string? AttachedId { get; set; }
    [MaxLength(1024)]    public string BlobUri { get; set; } = "";
    [MaxLength(512)]     public string Caption { get; set; } = "";
    [MaxLength(256)]     public string TakenByEmail { get; set; } = "";
    public DateTimeOffset TakenAt { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
}
