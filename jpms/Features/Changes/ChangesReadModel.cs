using Jewel.JPMS.Contracts.Changes;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Changes;

public sealed class ChangesReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<ChangeRecord>> changesByProject = new();

    public ChangesReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<ChangeRecord> Current(string projectId) =>
        changesByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<ChangeRecord>();

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        changesByProject[projectId] = await queries.AskAsync(new ListChangesForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
