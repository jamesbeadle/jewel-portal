using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

// The close gate. Considers the agents predefined for the record's type (provisioned on demand) and
// asks each whether its work is complete. If any disagrees the request stays open and the outcome
// carries the blockers; only when all agree does the request move to Closed. The Requests Agent is
// non-blocking, so a plain request still closes as it did before agents became type-derived.
public sealed class AttemptCloseRequestHandler : ICommandHandler<AttemptCloseRequest, RequestCloseOutcome>
{
    private readonly JpmsContext context;
    private readonly AgentRegistry registry;
    private readonly AgentProvisioning provisioning;
    public AttemptCloseRequestHandler(JpmsContext context, AgentRegistry registry, AgentProvisioning provisioning)
    { this.context = context; this.registry = registry; this.provisioning = provisioning; }

    public async Task<RequestCloseOutcome> HandleAsync(AttemptCloseRequest command, CancellationToken cancellationToken)
    {
        var request = await context.Requests
            .FirstOrDefaultAsync(r => r.RequestId == command.RequestId, cancellationToken);
        if (request is null)
            return new RequestCloseOutcome(Closed: false, BlockingAgents: Array.Empty<AgentCompletionState>());

        var watches = await provisioning.EnsureProvisionedAsync(command.RequestId, RecordType.Request, cancellationToken);

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

        request.Status = (int)RequestStatus.Closed;
        await context.SaveChangesAsync(cancellationToken);
        return new RequestCloseOutcome(Closed: true, BlockingAgents: Array.Empty<AgentCompletionState>());
    }
}
