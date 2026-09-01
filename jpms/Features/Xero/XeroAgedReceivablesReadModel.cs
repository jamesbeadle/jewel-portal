using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Features.Xero;

public sealed class XeroAgedReceivablesReadModel : IReadModelStore<XeroAgedReceivablesSnapshot>
{
    private readonly IQueryClient queries;

    public XeroAgedReceivablesReadModel(IQueryClient queries) { this.queries = queries; }

    public XeroAgedReceivablesSnapshot? Current { get; private set; }

    public event Action? OnChanged;

    public Task RefreshAsync(CancellationToken cancellationToken) => RefreshAsync(false, cancellationToken);

    /// <summary>Force bypasses the API's short-lived Xero cache — used by the explicit Refresh button.</summary>
    public async Task RefreshAsync(bool force, CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new GetXeroAgedReceivables(force), cancellationToken);
        OnChanged?.Invoke();
    }
}
