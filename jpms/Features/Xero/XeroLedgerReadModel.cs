using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Features.Xero;

/// <summary>
/// The Xero ledger, cached per allocation status, plus the tab-bar counts.
///
/// It used to hold one list: every ledger line the business had, refetched on every visit to the
/// allocation page. That page is a tab per status and only ever renders one of them, so the cache
/// is keyed the same way — the browser now holds the tab you are on (and the unallocated queue the
/// tab bar is built from) instead of the whole ledger. Counts for the tabs you are NOT on come
/// from a GROUP BY on the server.
///
/// It deliberately no longer implements IReadModelStore: that interface's single unkeyed
/// Current/RefreshAsync pair is the shape this was moving away from.
/// </summary>
public sealed class XeroLedgerReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<XeroAllocationStatus, IReadOnlyList<XeroLedgerLine>> linesByStatus = new();

    public XeroLedgerReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    /// <summary>The cached lines for one status, or null if that status has not been loaded yet.</summary>
    public IReadOnlyList<XeroLedgerLine>? Current(XeroAllocationStatus status) =>
        linesByStatus.TryGetValue(status, out var lines) ? lines : null;

    /// <summary>The tab-bar counts, or null before the first load.</summary>
    public XeroLedgerCounts? Counts { get; private set; }

    public async Task RefreshAsync(XeroAllocationStatus status, CancellationToken cancellationToken)
    {
        linesByStatus[status] = await queries.AskAsync(new ListXeroLedgerLines(status), cancellationToken);
        OnChanged?.Invoke();
    }

    public async Task RefreshCountsAsync(CancellationToken cancellationToken)
    {
        Counts = await queries.AskAsync(new GetXeroLedgerCounts(), cancellationToken);
        OnChanged?.Invoke();
    }

    /// <summary>Which statuses currently hold data — the set a write has to reload.</summary>
    public IReadOnlyList<XeroAllocationStatus> LoadedStatuses => linesByStatus.Keys.ToList();
}
