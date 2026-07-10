using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

/// <summary>
/// Amends a valuation invoice's period/amount. Allowed while Raised or Rejected (an amendment of a
/// Rejected invoice returns it to Raised, ready to resubmit) — and at any status for a manual
/// invoice, since correcting historic figures is the point of manual entries. Manual invoices may
/// also revise the paid amount and backdated dates; certified/paid totals and any Preapproved
/// claim's frozen figures are recomputed in the same operation.
/// </summary>
public sealed record UpdateValuationInvoice(
    string ValuationInvoiceId,
    DateTimeOffset PeriodMonth,
    decimal Amount,
    decimal? AmountPaid = null,      // manual only
    DateTimeOffset? IssuedAt = null, // manual only
    DateTimeOffset? PaidAt = null,   // manual only
    string? Note = null) : ICommand<ValuationInvoice>;
