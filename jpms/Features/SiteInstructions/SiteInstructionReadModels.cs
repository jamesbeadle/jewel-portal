using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Features.SiteInstructions;

public sealed class SiteInstructionReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<SiteInstruction>> rowsByProject = new();

    public SiteInstructionReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<SiteInstruction> Current(string projectId) =>
        rowsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<SiteInstruction>();
    /// <summary>True once this key's rows have landed. Current(...) answers with an empty list
    /// until then, which is indistinguishable from a real empty result — so anything rendering a
    /// row count or an empty state must gate on this first.</summary>
    public bool LoadedFor(string projectId) => rowsByProject.ContainsKey(projectId);

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        rowsByProject[projectId] = await queries.AskAsync(new ListSiteInstructionsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
