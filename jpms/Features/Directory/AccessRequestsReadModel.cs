using Jewel.JPMS.Contracts.AccessRequests;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Directory;

public sealed class AccessRequestsReadModel : IReadModelStore<IReadOnlyList<AccessRequest>>
{
    private readonly IQueryClient queries;

    public AccessRequestsReadModel(IQueryClient queries) { this.queries = queries; }

    public IReadOnlyList<AccessRequest>? Current { get; private set; }

    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListPendingAccessRequests(), cancellationToken);
        OnChanged?.Invoke();
    }
}
