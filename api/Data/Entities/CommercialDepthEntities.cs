using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class ClaimPeriodEntity
{
    [Key, MaxLength(64)] public string ClaimPeriodId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int PeriodNumber { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
}

public sealed class ForecastComponentEntity
{
    [Key, MaxLength(64)] public string ForecastComponentId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(128)]     public string PackageName { get; set; } = "";
    public decimal CostIncurred { get; set; }
    public decimal CostCommitted { get; set; }
    public decimal QsAccrualAmount { get; set; }
    public decimal PrelimForecast { get; set; }
    public decimal CostToComplete { get; set; }
}

public sealed class QsAccrualEntity
{
    [Key, MaxLength(64)] public string QsAccrualId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(128)]     public string Category { get; set; } = "";
    [MaxLength(1024)]    public string Description { get; set; } = "";
    public decimal AddAmount { get; set; }
    public decimal OmitAmount { get; set; }
    public decimal LiabilityAmount { get; set; }
    [MaxLength(256)]     public string SignedOffByEmail { get; set; } = "";
    public DateTimeOffset SignedOffAt { get; set; }
}

public sealed class PrelimItemEntity
{
    [Key, MaxLength(64)] public string PrelimItemId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string Description { get; set; } = "";
}

public sealed class PrelimForecastEntryEntity
{
    [Key, MaxLength(64)] public string PrelimForecastEntryId { get; set; } = "";
    [MaxLength(64)]      public string PrelimItemId { get; set; } = "";
    public int WeekNumber { get; set; }
    public decimal TenderedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal ForecastAmount { get; set; }
}

public sealed class EotEntity
{
    [Key, MaxLength(64)] public string EotId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(1024)]    public string Reason { get; set; } = "";
    public int DaysGranted { get; set; }
    public decimal CommercialRecovery { get; set; }
    public DateTimeOffset GrantedAt { get; set; }
}

public sealed class CostCodeEntity
{
    [Key, MaxLength(64)] public string CostCodeId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(32)]      public string Code { get; set; } = "";
    [MaxLength(256)]     public string Description { get; set; } = "";
}
