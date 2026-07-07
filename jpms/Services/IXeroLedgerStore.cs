using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Services;

public interface IXeroLedgerStore
{
    /// <summary>Stored ledger lines, or null while the first load is in flight.</summary>
    IReadOnlyList<XeroLedgerLine>? Lines();

    Task RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>Pulls the latest from Xero into the stored ledger, then refreshes the view.</summary>
    Task<XeroLedgerSyncResult> SyncAsync(CancellationToken cancellationToken = default);

    /// <summary>Applies an allocation action to a batch of lines, then refreshes. Returns lines affected.</summary>
    Task<int> ApplyAsync(SetXeroAllocation command, CancellationToken cancellationToken = default);

    event Action? OnChange;
}
