using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Features.Hs;

public sealed class HsRecordsReadModel : IReadModelStore<IReadOnlyList<HsRecord>>
{
    private readonly IQueryClient queries;
    public IReadOnlyList<HsRecord>? Current { get; private set; }
    public event Action? OnChanged;

    public HsRecordsReadModel(IQueryClient queries) { this.queries = queries; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListHsRecords(), cancellationToken);
        OnChanged?.Invoke();
    }
}
