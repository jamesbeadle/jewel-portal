using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Cqrs;

namespace Jewel.JPMS.Features.Xero;

public sealed class XeroTransactionsReadModel : IReadModelStore<XeroTransactionsSnapshot>
{
    private readonly IQueryClient queries;

    public XeroTransactionsReadModel(IQueryClient queries) { this.queries = queries; }

    public XeroTransactionsSnapshot? Current { get; private set; }

    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListXeroTransactions(), cancellationToken);
        OnChanged?.Invoke();
    }
}
