using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Closeout;

public sealed class DefectsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<Defect>> defectsByProject = new();

    public DefectsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<Defect> Current(string projectId) =>
        defectsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<Defect>();

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        defectsByProject[projectId] = await queries.AskAsync(new ListDefectsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
