using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

// Applies an agent to a request: adds the watch-queue row. Idempotent on (request, agent) — applying
// an already-applied agent returns the existing row. The stub's current blocking reason is cached on
// the row as StatusMessage so the queue can show it without re-evaluating.
public sealed class AssignAgentHandler : ICommandHandler<AssignAgent, RequestAgent>
{
    private readonly JpmsContext context;
    private readonly AgentRegistry registry;
    public AssignAgentHandler(JpmsContext context, AgentRegistry registry) { this.context = context; this.registry = registry; }

    public async Task<RequestAgent> HandleAsync(AssignAgent command, CancellationToken cancellationToken)
    {
        var agent = registry.Find(command.AgentKey)
            ?? throw new InvalidOperationException($"Unknown agent '{command.AgentKey}'.");

        var existing = await context.RequestAgents
            .FirstOrDefaultAsync(a => a.RequestId == command.RequestId && a.AgentKey == command.AgentKey, cancellationToken);
        if (existing is not null)
            return existing.ToModel(agent);

        var status = agent.EvaluateCompletion(AgentAssignmentStatus.Active);

        var entity = new RequestAgentEntity
        {
            RequestAgentId = AgentsIdentifierFactory.Next(),
            RequestId = command.RequestId,
            AgentKey = command.AgentKey,
            Status = (int)AgentAssignmentStatus.Active,
            IsPrimary = command.IsPrimary,
            StatusMessage = status.Message,
            AssignedByEmail = command.AssignedByEmail,
            AssignedAt = DateTimeOffset.UtcNow,
            CompletedAt = null
        };
        context.RequestAgents.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(agent);
    }
}
