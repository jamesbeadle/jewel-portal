using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Agents.Queries;

public sealed class ListRequestAgentsHandler : IQueryHandler<ListRequestAgents, IReadOnlyList<RequestAgent>>
{
    private readonly JpmsContext context;
    private readonly AgentRegistry registry;
    public ListRequestAgentsHandler(JpmsContext context, AgentRegistry registry) { this.context = context; this.registry = registry; }

    public async Task<IReadOnlyList<RequestAgent>> HandleAsync(ListRequestAgents query, CancellationToken cancellationToken)
    {
        var entities = await context.RequestAgents
            .Where(a => a.RequestId == query.RequestId)
            .OrderByDescending(a => a.IsPrimary)
            .ThenBy(a => a.AssignedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(e => e.ToModel(registry.Find(e.AgentKey))).ToList().AsReadOnly();
    }
}
