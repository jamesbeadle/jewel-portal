using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Features.Site;

public sealed class SiteReportsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<SiteReport>> reportsByProject = new();

    public SiteReportsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<SiteReport> Current(string projectId) =>
        reportsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<SiteReport>();
    /// <summary>True once this key's rows have landed. Current(...) answers with an empty list
    /// until then, which is indistinguishable from a real empty result — so anything rendering a
    /// figure, a row count or an empty state must gate on this first.</summary>
    public bool LoadedFor(string projectId) => reportsByProject.ContainsKey(projectId);

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        reportsByProject[projectId] = await queries.AskAsync(new ListSiteReportsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}

public sealed class ProgrammeReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<ProgrammeTask>> tasksByProject = new();

    public ProgrammeReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<ProgrammeTask> Current(string projectId) =>
        tasksByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<ProgrammeTask>();
    /// <summary>True once this key's rows have landed. Current(...) answers with an empty list
    /// until then, which is indistinguishable from a real empty result — so anything rendering a
    /// figure, a row count or an empty state must gate on this first.</summary>
    public bool LoadedFor(string projectId) => tasksByProject.ContainsKey(projectId);

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        tasksByProject[projectId] = await queries.AskAsync(new GetProgrammeForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
