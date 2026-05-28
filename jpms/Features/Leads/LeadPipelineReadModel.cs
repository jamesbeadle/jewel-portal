using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Leads;

public sealed class LeadPipelineReadModel : IReadModelStore<IReadOnlyList<Lead>>
{
    private readonly IQueryClient queries;

    public LeadPipelineReadModel(IQueryClient queries) { this.queries = queries; }

    public IReadOnlyList<Lead>? Current { get; private set; }

    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListLeadsInPipeline(), cancellationToken);
        OnChanged?.Invoke();
    }
}
