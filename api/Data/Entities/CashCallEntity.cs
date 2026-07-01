using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// A monthly cash call (see Jewel.JPMS.Models.CashCall). When Received, its AmountReceived is added to
// ProjectEntity.CashCallTotal.
public sealed class CashCallEntity
{
    [Key, MaxLength(64)] public string CashCallId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string? ValuationClaimId { get; set; }
    public int Number { get; set; }
    [MaxLength(32)]      public string Reference { get; set; } = "";
    public DateTimeOffset PeriodMonth { get; set; }
    public decimal AmountRequested { get; set; }
    public decimal AmountReceived { get; set; }
    public int Status { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? InvoicedAt { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
}
