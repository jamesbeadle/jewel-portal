using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Cqrs;

namespace Jewel.JPMS.Features.BuildingControl;

public sealed class BuildingControlReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, BuildingControlProjectView> viewByProject = new();

    public BuildingControlReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    /// <summary>The project's whole building control picture — cases, inspections, files — or
    /// null before the first fetch lands. The tab and the inspection detail page both read this
    /// one answer.</summary>
    public BuildingControlProjectView? Current(string projectId) =>
        viewByProject.TryGetValue(projectId, out var view) ? view : null;

    /// <summary>True once this key's view has landed. Current(...) answers null until then —
    /// anything rendering a figure, a row count or an empty state must gate on this first.</summary>
    public bool LoadedFor(string projectId) => viewByProject.ContainsKey(projectId);

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        viewByProject[projectId] = await queries.AskAsync(new GetBuildingControlForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
