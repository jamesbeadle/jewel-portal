using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

/// <summary>
/// Withdraws a Raised or Rejected valuation invoice (-> Cancelled). Kept for the audit trail but
/// excluded from every total; its snapshots are flagged superseded. Use Delete to remove an
/// invoice entirely.
/// </summary>
public sealed record CancelValuationInvoice(
    string ValuationInvoiceId,
    string? Note = null) : ICommand<ValuationInvoice>;
