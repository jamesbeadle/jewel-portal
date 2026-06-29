using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Agents.Queries;

public sealed class ListAgentProposalsHandler : IQueryHandler<ListAgentProposals, IReadOnlyList<AgentProposal>>
{
    private readonly JpmsContext context;
    private readonly AgentRegistry registry;
    public ListAgentProposalsHandler(JpmsContext context, AgentRegistry registry) { this.context = context; this.registry = registry; }

    public async Task<IReadOnlyList<AgentProposal>> HandleAsync(ListAgentProposals query, CancellationToken cancellationToken)
    {
        var entities = await context.AgentProposals
            .Where(p => p.RequestId == query.RequestId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(e => e.ToModel(registry.Find(e.AgentKey))).ToList().AsReadOnly();
    }
}
