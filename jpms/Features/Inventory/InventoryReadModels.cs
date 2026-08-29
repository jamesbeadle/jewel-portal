using Jewel.JPMS.Contracts.Inventory;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Inventory;

public sealed class InventoryReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<InventoryItem>> itemsByProject = new();

    public InventoryReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<InventoryItem> Current(string projectId) =>
        itemsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<InventoryItem>();
    /// <summary>True once this key's rows have landed. Current(...) answers with an empty list
    /// until then, which is indistinguishable from a real empty result — so anything rendering a
    /// figure, a row count or an empty state must gate on this first.</summary>
    public bool LoadedFor(string projectId) => itemsByProject.ContainsKey(projectId);

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        itemsByProject[projectId] = await queries.AskAsync(new ListInventoryForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
