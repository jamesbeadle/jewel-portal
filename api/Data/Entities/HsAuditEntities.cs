using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One run of the H&S inspection framework on a project (2026-09-15, Katy-Louise's audit
/// workbook brought into the portal). String-keyed with no FKs — the building-control
/// arrangement; the handlers own the cascades. Number is minted per project, like a work order.
/// </summary>
public sealed class HsAuditEntity
{
    [Key, MaxLength(64)] public string HsAuditId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int Number { get; set; }
    public int Status { get; set; }
    public int Type { get; set; }
    public DateTimeOffset InspectionDate { get; set; }
    [MaxLength(256)]     public string SiteManagerName { get; set; } = "";
    [MaxLength(256)]     public string SafetyOfficerName { get; set; } = "";
    [MaxLength(4000)]    public string SummaryOfWorkActivities { get; set; } = "";
    public int? SiteOperativeCount { get; set; }
    public string FurtherComments { get; set; } = "";
    public decimal? Score { get; set; }
    public decimal? PreviousScore { get; set; }
    [MaxLength(32)]      public string TemplateVersion { get; set; } = "";
    [MaxLength(256)]     public string ManagerName { get; set; } = "";
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Reference => $"HSA-{Number:0000}";
}

/// <summary>One line of the framework on one audit — the template item plus what the officer
/// found. DisplayOrder is the template's order, so the form renders without re-sorting codes.</summary>
public sealed class HsAuditItemEntity
{
    [Key, MaxLength(64)] public string HsAuditItemId { get; set; } = "";
    [MaxLength(64)]      public string HsAuditId { get; set; } = "";
    [MaxLength(16)]      public string Code { get; set; } = "";
    public int Section { get; set; }
    [MaxLength(256)]     public string Name { get; set; } = "";
    public int DisplayOrder { get; set; }
    public int? Comment { get; set; }
    public int? Rate { get; set; }
    public int? Class { get; set; }
    public int Minus { get; set; }
    public int? TimeScale { get; set; }
    public string Findings { get; set; } = "";
    [MaxLength(256)]     public string OwnerName { get; set; } = "";
    public DateTimeOffset? DateRectified { get; set; }
    [MaxLength(64)]      public string? HsRecordId { get; set; }
}
