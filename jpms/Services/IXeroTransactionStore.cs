using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Services;

public interface IXeroTransactionStore
{
    /// <summary>Latest snapshot from Xero, or null while the first load is in flight.</summary>
    XeroTransactionsSnapshot? Snapshot();

    /// <summary>Forces a fresh read of Xero (stale-while-revalidate on tab entry, or the Refresh button).</summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);

    event Action? OnChange;
}
