using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// A monthly valuation invoice (see Jewel.JPMS.Models.ValuationInvoice). When Paid, its AmountPaid is added to
// ProjectEntity.ValuationInvoicePaidTotal.
public sealed class ValuationInvoiceEntity
{
    [Key, MaxLength(64)] public string ValuationInvoiceId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string? ValuationClaimId { get; set; }
    public int Number { get; set; }
    [MaxLength(32)]      public string Reference { get; set; } = "";
    public DateTimeOffset PeriodMonth { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public int Status { get; set; }
    public DateTimeOffset RaisedAt { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}
