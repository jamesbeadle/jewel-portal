using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IValuationInvoiceStore
{
    event Action? OnChange;

    Task<IReadOnlyList<ValuationInvoice>> ListAsync(string projectId, CancellationToken cancellationToken = default);
    Task<ProjectValuationInvoiceSummary> GetSummaryAsync(string projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ValuationInvoiceEvent>> ListEventsAsync(string valuationInvoiceId, CancellationToken cancellationToken = default);

    Task<ValuationInvoice> CreateAsync(string projectId, DateTimeOffset periodMonth, decimal amount, string? valuationClaimId, CancellationToken cancellationToken = default);

    /// <summary>Backdated historic entry, recorded directly as Issued (or Paid when a paid
    /// amount is given) so receipts-to-date can be brought current. Counts toward
    /// "Certified to date" immediately.</summary>
    Task<ValuationInvoice> CreateManualAsync(string projectId, DateTimeOffset periodMonth, decimal amount, decimal? amountPaid, DateTimeOffset? issuedAt, DateTimeOffset? paidAt, string? note, CancellationToken cancellationToken = default);

    Task<ValuationInvoice> UpdateAsync(string valuationInvoiceId, DateTimeOffset periodMonth, decimal amount, decimal? amountPaid = null, DateTimeOffset? issuedAt = null, DateTimeOffset? paidAt = null, string? note = null, CancellationToken cancellationToken = default);

    // Approval workflow: Raised → Submitted → Approved/Rejected; Raised/Rejected → Cancelled.
    Task<ValuationInvoice> SubmitAsync(string valuationInvoiceId, CancellationToken cancellationToken = default);
    Task<ValuationInvoice> ApproveAsync(string valuationInvoiceId, string? note = null, CancellationToken cancellationToken = default);
    Task<ValuationInvoice> RejectAsync(string valuationInvoiceId, string reason, CancellationToken cancellationToken = default);
    Task<ValuationInvoice> CancelAsync(string valuationInvoiceId, string? note = null, CancellationToken cancellationToken = default);

    Task<ValuationInvoice> IssueAsync(string valuationInvoiceId, CancellationToken cancellationToken = default);
    Task<ValuationInvoice> RecordPaymentAsync(string valuationInvoiceId, decimal amountPaid, CancellationToken cancellationToken = default);
    Task DeleteAsync(string valuationInvoiceId, CancellationToken cancellationToken = default);
}
