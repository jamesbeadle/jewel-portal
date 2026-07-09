using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One share of an allocated Xero ledger line. Present only when a line's value
/// is split — across cost centres, projects, or both; a whole-line allocation
/// stays on XeroLedgerLineEntity.ProjectId + CostCenterCode with no split rows.
/// Each row carries its own ProjectId, so one supplier line can fund several
/// projects. Net follows the parent line's convention: pre-VAT and stored
/// positive (the line's Type applies the sign). The rows for a line always sum
/// exactly to the line's Net at the moment of allocation; re-allocating or
/// resetting the line replaces or removes them.
/// </summary>
public sealed class XeroCostSplitEntity
{
    // "{XeroLedgerLineId}:{ProjectId}:{CostCenterCode}" — a line never carries the
    // same project + centre combination twice, so the natural key doubles as a
    // uniqueness guarantee.
    [Key, MaxLength(256)] public string XeroCostSplitId { get; set; } = "";
    [MaxLength(140)]      public string XeroLedgerLineId { get; set; } = "";
    [MaxLength(64)]       public string ProjectId { get; set; } = "";
    [MaxLength(32)]       public string CostCenterCode { get; set; } = "";
    public decimal Net { get; set; }
}
