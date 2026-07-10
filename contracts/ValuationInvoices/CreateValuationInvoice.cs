using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

/// <summary>
/// Raises a monthly valuation invoice against a project (optionally drawn from a valuation claim).
/// Created in the Raised state — unless <see cref="IsManual"/> is set, which records a backdated
/// historical invoice directly as Issued (or Paid when a paid amount/date is supplied) so
/// receipts-to-date can be brought current; manual invoices count fully toward "Certified to date"
/// and Total Paid but never enter the approval loop.
/// </summary>
public sealed record CreateValuationInvoice(
    string ProjectId,
    DateTimeOffset PeriodMonth,
    decimal Amount,
    string? ValuationClaimId = null,
    bool IsManual = false,
    decimal? AmountPaid = null,      // manual only; > 0 makes the invoice Paid
    DateTimeOffset? IssuedAt = null, // manual only; backdated issue date (defaults to PeriodMonth)
    DateTimeOffset? PaidAt = null,   // manual only; backdated payment date
    string? Note = null) : ICommand<ValuationInvoice>; // e.g. original invoice ref from the old system
