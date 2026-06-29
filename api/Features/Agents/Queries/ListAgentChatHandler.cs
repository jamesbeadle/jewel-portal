using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Agents.Queries;

public sealed class ListAgentChatHandler : IQueryHandler<ListAgentChat, IReadOnlyList<AgentChatMessage>>
{
    private readonly JpmsContext context;
    public ListAgentChatHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<AgentChatMessage>> HandleAsync(ListAgentChat query, CancellationToken cancellationToken)
    {
        var entities = await context.AgentChatMessages
            .Where(m => m.RequestId == query.RequestId && m.AgentKey == query.AgentKey)
            .OrderBy(m => m.PostedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(e => e.ToModel()).ToList().AsReadOnly();
    }
}
