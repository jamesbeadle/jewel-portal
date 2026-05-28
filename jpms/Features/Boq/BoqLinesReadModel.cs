using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Boq;

public sealed class BoqLinesReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<BoqLineItem>> linesByProject = new();

    public BoqLinesReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<BoqLineItem> Current(string projectId) =>
        linesByProject.TryGetValue(projectId, out var lines) ? lines : Array.Empty<BoqLineItem>();

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        linesByProject[projectId] = await queries.AskAsync(new ListBoqLinesForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
