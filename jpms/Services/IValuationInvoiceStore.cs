using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IValuationInvoiceStore
{
    event Action? OnChange;

    Task<IReadOnlyList<ValuationInvoice>> ListAsync(string projectId, CancellationToken cancellationToken = default);
    Task<ProjectValuationInvoiceSummary> GetSummaryAsync(string projectId, CancellationToken cancellationToken = default);
    Task<ValuationInvoice> CreateAsync(string projectId, DateTimeOffset periodMonth, decimal amount, string? valuationClaimId, CancellationToken cancellationToken = default);
    Task<ValuationInvoice> IssueAsync(string valuationInvoiceId, CancellationToken cancellationToken = default);
    Task<ValuationInvoice> RecordPaymentAsync(string valuationInvoiceId, decimal amountPaid, CancellationToken cancellationToken = default);
}
