using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Agents.Queries;

// The global watch queue: every (request, agent) pair, flattened with request context so the queue
// page renders without a second lookup. Joined in-memory by id (no FK constraints, matching JPMS).
public sealed class ListAgentQueueHandler : IQueryHandler<ListAgentQueue, IReadOnlyList<AgentQueueItem>>
{
    private readonly JpmsContext context;
    private readonly AgentRegistry registry;
    public ListAgentQueueHandler(JpmsContext context, AgentRegistry registry) { this.context = context; this.registry = registry; }

    public async Task<IReadOnlyList<AgentQueueItem>> HandleAsync(ListAgentQueue query, CancellationToken cancellationToken)
    {
        var watches = await context.RequestAgents
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(cancellationToken);

        if (watches.Count == 0) return Array.Empty<AgentQueueItem>();

        var requestIds = watches.Select(w => w.RequestId).Distinct().ToList();
        var requests = await context.Requests
            .Where(r => requestIds.Contains(r.RequestId))
            .ToDictionaryAsync(r => r.RequestId, cancellationToken);

        var items = new List<AgentQueueItem>(watches.Count);
        foreach (var w in watches)
        {
            requests.TryGetValue(w.RequestId, out var r);
            var agent = registry.Find(w.AgentKey);
            items.Add(new AgentQueueItem(
                RequestAgentId: w.RequestAgentId,
                RequestId: w.RequestId,
                ProjectId: r?.ProjectId ?? "",
                RequestNumber: r?.Number ?? 0,
                RequestTitle: r?.Title ?? "(unknown request)",
                RequestStatus: r is null ? RequestStatus.Open : (RequestStatus)r.Status,
                AgentKey: w.AgentKey,
                DisplayName: agent?.DisplayName ?? w.AgentKey,
                Discipline: agent?.Discipline ?? AgentDiscipline.Commercial,
                Status: (AgentAssignmentStatus)w.Status,
                IsPrimary: w.IsPrimary,
                StatusMessage: w.StatusMessage,
                AssignedAt: w.AssignedAt));
        }
        return items.AsReadOnly();
    }
}
