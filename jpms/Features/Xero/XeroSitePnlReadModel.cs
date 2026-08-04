using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Cqrs;

namespace Jewel.JPMS.Features.Xero;

/// <summary>
/// The stored site P&L (monthly income/cost per project from Xero, synced nightly) behind the
/// Profit Summary's cumulative invoiced-vs-cost chart. A database read on the API side — the
/// explicit Refresh button sends SyncXeroSitePnl first, then refreshes this.
/// </summary>
public sealed class XeroSitePnlReadModel : IReadModelStore<XeroSitePnlSnapshot>
{
    private readonly IQueryClient queries;

    public XeroSitePnlReadModel(IQueryClient queries) { this.queries = queries; }

    public XeroSitePnlSnapshot? Current { get; private set; }

    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new GetXeroSitePnl(), cancellationToken);
        OnChanged?.Invoke();
    }
}
