using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// The client's own reference for one of our cost centres on one project — the item number in
/// the architect's schedule of works the client reconciles our valuation against. Keyed on
/// (ProjectId, CostCode): the same cost centre carries a different reference on every job.
/// Frozen onto each valuation report snapshot line at capture; never read back by the client.
/// </summary>
public sealed class ClientCostReferenceEntity
{
    [Key, MaxLength(64)] public string ClientCostReferenceId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(32)]      public string CostCode { get; set; } = "";
    [MaxLength(64)]      public string ClientReference { get; set; } = "";
}
