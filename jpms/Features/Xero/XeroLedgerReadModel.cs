using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Cqrs;

namespace Jewel.JPMS.Features.Xero;

public sealed class XeroLedgerReadModel : IReadModelStore<IReadOnlyList<XeroLedgerLine>>
{
    private readonly IQueryClient queries;

    public XeroLedgerReadModel(IQueryClient queries) { this.queries = queries; }

    public IReadOnlyList<XeroLedgerLine>? Current { get; private set; }

    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListXeroLedgerLines(), cancellationToken);
        OnChanged?.Invoke();
    }
}
