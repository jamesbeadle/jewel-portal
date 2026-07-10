using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One work order's share of an allocated Xero purchase line — the line-to-order tie
/// behind the Financials tab and WO Allocation tab. A bill that pays one order carries
/// a single row for its full net; a bill paying several orders at once (a subcontractor
/// invoicing a main order plus variation orders together) carries one row per order.
/// Amount is signed like the line's net (credit notes negative). Rows only ever exist
/// on whole-line allocations to the order's project; the unique (line, order) index
/// stops the same order being sliced twice on one line.
/// </summary>
public sealed class XeroLineWorkOrderLinkEntity
{
    [Key, MaxLength(64)] public string XeroLineWorkOrderLinkId { get; set; } = "";
    [MaxLength(140)]     public string XeroLedgerLineId { get; set; } = "";
    [MaxLength(64)]      public string WorkOrderId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public decimal Amount { get; set; }
}
