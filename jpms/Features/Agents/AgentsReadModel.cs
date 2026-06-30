using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Agents;

public sealed class AgentsReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<RequestAgent>> agentsByRequest = new();
    private IReadOnlyList<AgentQueueItem> queue = Array.Empty<AgentQueueItem>();

    public AgentsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<AgentQueueItem> Queue => queue;

    public IReadOnlyList<RequestAgent> ForRequest(string requestId) =>
        agentsByRequest.TryGetValue(requestId, out var list) ? list : Array.Empty<RequestAgent>();

    public async Task RefreshRequestAsync(string requestId, CancellationToken cancellationToken)
    {
        agentsByRequest[requestId] = await queries.AskAsync(new ListRequestAgents(requestId), cancellationToken);
        OnChanged?.Invoke();
    }

    public async Task RefreshQueueAsync(CancellationToken cancellationToken)
    {
        queue = await queries.AskAsync(new ListAgentQueue(), cancellationToken);
        OnChanged?.Invoke();
    }
}
