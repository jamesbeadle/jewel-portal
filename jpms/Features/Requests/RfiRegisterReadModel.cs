using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Requests;

// Backs the cross-project RFI dashboard: one flat list of every RFI on every live project.
// The page awaits RefreshAsync once from OnInitializedAsync (never from render), matching the
// other top-level read models — cached rows render immediately on a revisit, then update when
// the reload lands.
public sealed class RfiRegisterReadModel : IReadModelStore<IReadOnlyList<Request>>
{
    private readonly IQueryClient queries;

    public RfiRegisterReadModel(IQueryClient queries) { this.queries = queries; }

    public IReadOnlyList<Request>? Current { get; private set; }

    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListRfisAcrossProjects(), cancellationToken);
        OnChanged?.Invoke();
    }
}
