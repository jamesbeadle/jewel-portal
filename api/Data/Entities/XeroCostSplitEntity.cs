using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One cost-centre share of an allocated Xero ledger line. Present only when a
/// line's value is split across MULTIPLE cost centres — a whole-line allocation
/// stays on XeroLedgerLineEntity.CostCenterCode with no split rows. Net follows
/// the parent line's convention: pre-VAT and stored positive (the line's Type
/// applies the sign). The rows for a line always sum exactly to the line's Net
/// at the moment of allocation; re-allocating or resetting the line replaces or
/// removes them.
/// </summary>
public sealed class XeroCostSplitEntity
{
    // "{XeroLedgerLineId}:{CostCenterCode}" — a line never carries the same
    // centre twice, so the natural key doubles as a uniqueness guarantee.
    [Key, MaxLength(180)] public string XeroCostSplitId { get; set; } = "";
    [MaxLength(140)]      public string XeroLedgerLineId { get; set; } = "";
    [MaxLength(32)]       public string CostCenterCode { get; set; } = "";
    public decimal Net { get; set; }
}
