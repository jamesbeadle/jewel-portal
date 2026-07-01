using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface ICashCallStore
{
    event Action? OnChange;

    Task<IReadOnlyList<CashCall>> ListAsync(string projectId, CancellationToken cancellationToken = default);
    Task<ProjectCashCallSummary> GetSummaryAsync(string projectId, CancellationToken cancellationToken = default);
    Task<CashCall> CreateAsync(string projectId, DateTimeOffset periodMonth, decimal amountRequested, string? valuationClaimId, CancellationToken cancellationToken = default);
    Task<CashCall> IssueInvoiceAsync(string cashCallId, CancellationToken cancellationToken = default);
    Task<CashCall> RecordReceiptAsync(string cashCallId, decimal amountReceived, CancellationToken cancellationToken = default);
}
