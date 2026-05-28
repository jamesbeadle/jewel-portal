using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Site;

public sealed class SiteReportsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<SiteReport>> reportsByProject = new();

    public SiteReportsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<SiteReport> Current(string projectId) =>
        reportsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<SiteReport>();

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

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        tasksByProject[projectId] = await queries.AskAsync(new GetProgrammeForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
