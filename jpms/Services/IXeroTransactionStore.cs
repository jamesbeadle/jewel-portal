using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Services;

public interface IXeroTransactionStore
{
    /// <summary>Latest snapshot from Xero, or null while the first load is in flight.</summary>
    XeroTransactionsSnapshot? Snapshot();

    /// <summary>
    /// Reloads from the API (stale-while-revalidate on tab entry). <paramref name="force"/> also
    /// bypasses the API's short-lived Xero cache — used by the explicit Refresh button.
    /// </summary>
    Task RefreshAsync(bool force = false, CancellationToken cancellationToken = default);

    event Action? OnChange;
}
