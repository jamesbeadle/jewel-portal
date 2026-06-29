using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

// The close gate. Loads every agent applied to the request and asks each whether its work is complete.
// If any disagrees the request stays open and the outcome carries the blockers; only when all agree
// (or none are applied) does the request move to Closed. With stub agents any applied agent blocks.
public sealed class AttemptCloseRequestHandler : ICommandHandler<AttemptCloseRequest, RequestCloseOutcome>
{
    private readonly JpmsContext context;
    private readonly AgentRegistry registry;
    public AttemptCloseRequestHandler(JpmsContext context, AgentRegistry registry) { this.context = context; this.registry = registry; }

    public async Task<RequestCloseOutcome> HandleAsync(AttemptCloseRequest command, CancellationToken cancellationToken)
    {
        var watches = await context.RequestAgents
            .Where(a => a.RequestId == command.RequestId)
            .ToListAsync(cancellationToken);

        var blocking = new List<AgentCompletionState>();
        foreach (var watch in watches)
        {
            var agent = registry.Find(watch.AgentKey);
            if (agent is null)
                continue; // an agent that no longer exists can't block the close

            var state = agent.EvaluateCompletion((AgentAssignmentStatus)watch.Status);
            if (!state.IsComplete)
                blocking.Add(state);
        }

        if (blocking.Count > 0)
            return new RequestCloseOutcome(Closed: false, BlockingAgents: blocking);

        var request = await context.Requests
            .FirstOrDefaultAsync(r => r.RequestId == command.RequestId, cancellationToken);
        if (request is null)
            return new RequestCloseOutcome(Closed: false, BlockingAgents: Array.Empty<AgentCompletionState>());

        request.Status = (int)RequestStatus.Closed;
        await context.SaveChangesAsync(cancellationToken);
        return new RequestCloseOutcome(Closed: true, BlockingAgents: Array.Empty<AgentCompletionState>());
    }
}
