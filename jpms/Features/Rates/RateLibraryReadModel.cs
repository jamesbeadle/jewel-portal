using Jewel.JPMS.Contracts.Rates;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Rates;

public sealed class RateLibraryReadModel : IReadModelStore<IReadOnlyList<Rate>>
{
    private readonly IQueryClient queries;

    public RateLibraryReadModel(IQueryClient queries) { this.queries = queries; }

    public IReadOnlyList<Rate>? Current { get; private set; }

    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListRatesInLibrary(), cancellationToken);
        OnChanged?.Invoke();
    }
}
