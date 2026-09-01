using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Features.Requests;

public sealed class RequestsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<Request>> requestsByProject = new();

    public RequestsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<Request> Current(string projectId) =>
        requestsByProject.TryGetValue(projectId, out var list) ? list : Array.Empty<Request>();
    /// <summary>True once this key's rows have landed. Current(...) answers with an empty list
    /// until then, which is indistinguishable from a real empty result — so anything rendering a
    /// figure, a row count or an empty state must gate on this first.</summary>
    public bool LoadedFor(string projectId) => requestsByProject.ContainsKey(projectId);

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        requestsByProject[projectId] = await queries.AskAsync(new ListRequestsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }
}
