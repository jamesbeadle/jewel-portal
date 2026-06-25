using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Requests;

public sealed class RequestsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<Request>> requestsByProject = new();

    public RequestsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<Request> Current(string projectId) =>
        requestsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<Request>();

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        requestsByProject[projectId] = await queries.AskAsync(new ListRequestsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
