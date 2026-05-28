using Jewel.JPMS.Contracts.Mobilisation;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Mobilisation;

public sealed class MobilisationChecklistReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, MobilisationChecklist> checklistsByProject = new();

    public MobilisationChecklistReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public MobilisationChecklist? Current(string projectId) =>
        checklistsByProject.TryGetValue(projectId, out var checklist) ? checklist : null;

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        checklistsByProject[projectId] = await queries.AskAsync(new GetMobilisationChecklistForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
