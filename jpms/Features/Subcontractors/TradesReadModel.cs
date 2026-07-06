using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Subcontractors;

public sealed class TradesReadModel : IReadModelStore<IReadOnlyList<Trade>>
{
    private readonly IQueryClient queries;
    public IReadOnlyList<Trade>? Current { get; private set; }
    public event Action? OnChanged;

    public TradesReadModel(IQueryClient queries) { this.queries = queries; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListTrades(), cancellationToken);
        OnChanged?.Invoke();
    }
}
