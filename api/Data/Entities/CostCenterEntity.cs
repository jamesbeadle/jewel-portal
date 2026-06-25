using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class CostCenterEntity
{
    [Key, MaxLength(64)] public string CostCenterId { get; set; } = "";
    [MaxLength(32)]      public string Code { get; set; } = "";
    [MaxLength(256)]     public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
