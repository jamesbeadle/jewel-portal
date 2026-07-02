using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Agents;

// Materialises the agent state rows a record is entitled to by its type. Applicability is predefined
// (AgentRegistry.ForRecordType) — this provisions the per-record state rows those agents need (chat,
// proposals, close-gate) the first time a record is looked at. Idempotent: only missing rows are added,
// so it is safe to call on every read and back-fills pre-existing records with no data migration.
//
// PROVISIONING DISABLED (Jul 2026): agents are parked while we decide whether attaching them to
// requests is the right model. EnsureProvisionedAsync no longer creates rows — it only returns
// whatever already exists (which, after the ClearRequestAgents migration, is nothing). To re-enable,
// set ProvisioningEnabled back to true.
public sealed class AgentProvisioning
{
    private static readonly bool ProvisioningEnabled = false;

    private readonly JpmsContext context;
    private readonly AgentRegistry registry;

    public AgentProvisioning(JpmsContext context, AgentRegistry registry)
    { this.context = context; this.registry = registry; }

    public async Task<IReadOnlyList<RequestAgentEntity>> EnsureProvisionedAsync(
        string recordId, RecordType type, CancellationToken cancellationToken)
    {
        var existing = await context.RequestAgents
            .Where(a => a.RequestId == recordId)
            .ToListAsync(cancellationToken);

        if (!ProvisioningEnabled) return existing;

        var have = existing.Select(e => e.AgentKey).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = false;
        foreach (var agent in registry.ForRecordType(type))
        {
            if (have.Contains(agent.Key)) continue;

            var state = agent.EvaluateCompletion(AgentAssignmentStatus.Active);
            var entity = new RequestAgentEntity
            {
                RequestAgentId = AgentsIdentifierFactory.Next(),
                RequestId = recordId,
                AgentKey = agent.Key,
                Status = (int)AgentAssignmentStatus.Active,
                IsPrimary = false,                  // no lead-agent concept under type-derived applicability
                StatusMessage = state.Message,
                AssignedByEmail = "system",         // provisioned by the system, not a human
                AssignedAt = DateTimeOffset.UtcNow,
                CompletedAt = null
            };
            context.RequestAgents.Add(entity);
            existing.Add(entity);
            added = true;
        }

        if (added) await context.SaveChangesAsync(cancellationToken);
        return existing;
    }
}
