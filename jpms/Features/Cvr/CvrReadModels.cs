using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Cvr;

public sealed class CvrSnapshotsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<CvrSnapshot>> snapshotsByProject = new();

    public CvrSnapshotsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<CvrSnapshot> Current(string projectId) =>
        snapshotsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<CvrSnapshot>();

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        snapshotsByProject[projectId] = await queries.AskAsync(new ListCvrSnapshotsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
