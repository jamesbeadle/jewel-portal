using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Subcontractors;

public sealed class SubcontractorsReadModel : IReadModelStore<IReadOnlyList<Subcontractor>>
{
    private readonly IQueryClient queries;
    public IReadOnlyList<Subcontractor>? Current { get; private set; }
    public event Action? OnChanged;

    public SubcontractorsReadModel(IQueryClient queries) { this.queries = queries; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListSubcontractors(), cancellationToken);
        OnChanged?.Invoke();
    }
}
