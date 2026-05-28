using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Projects;

public sealed class ProjectListReadModel : IReadModelStore<IReadOnlyList<Project>>
{
    private readonly IQueryClient queries;

    public ProjectListReadModel(IQueryClient queries) { this.queries = queries; }

    public IReadOnlyList<Project>? Current { get; private set; }

    public event Action? OnChanged;

    public Project? Find(string projectId) =>
        Current?.FirstOrDefault(project =>
            string.Equals(project.ProjectId, projectId, StringComparison.OrdinalIgnoreCase));

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListProjectsVisibleToUser(), cancellationToken);
        OnChanged?.Invoke();
    }
}
