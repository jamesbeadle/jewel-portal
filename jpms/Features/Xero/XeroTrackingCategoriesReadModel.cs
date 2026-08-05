using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Cqrs;

namespace Jewel.JPMS.Features.Xero;

public sealed class XeroTrackingCategoriesReadModel : IReadModelStore<XeroTrackingCategoriesSnapshot>
{
    private readonly IQueryClient queries;

    public XeroTrackingCategoriesReadModel(IQueryClient queries) { this.queries = queries; }

    public XeroTrackingCategoriesSnapshot? Current { get; private set; }

    public event Action? OnChanged;

    public Task RefreshAsync(CancellationToken cancellationToken) => RefreshAsync(false, cancellationToken);

    /// <summary>Force bypasses the API's short-lived Xero cache — used by the explicit Refresh button.</summary>
    public async Task RefreshAsync(bool force, CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListXeroTrackingCategories(force), cancellationToken);
        OnChanged?.Invoke();
    }
}
