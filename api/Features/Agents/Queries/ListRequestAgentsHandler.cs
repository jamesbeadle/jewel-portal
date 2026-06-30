using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Agents.Queries;

// The agents applicable to a record. Under type-derived applicability there is no assignment step:
// the predefined agents for the record's type are provisioned on first read (idempotent) and returned.
// Every record today is a Request, so the type is RecordType.Request.
public sealed class ListRequestAgentsHandler : IQueryHandler<ListRequestAgents, IReadOnlyList<RequestAgent>>
{
    private readonly JpmsContext context;
    private readonly AgentRegistry registry;
    private readonly AgentProvisioning provisioning;
    public ListRequestAgentsHandler(JpmsContext context, AgentRegistry registry, AgentProvisioning provisioning)
    { this.context = context; this.registry = registry; this.provisioning = provisioning; }

    public async Task<IReadOnlyList<RequestAgent>> HandleAsync(ListRequestAgents query, CancellationToken cancellationToken)
    {
        // Don't conjure agent rows for a record that doesn't exist.
        var exists = await context.Requests.AnyAsync(r => r.RequestId == query.RequestId, cancellationToken);
        if (!exists) return Array.Empty<RequestAgent>();

        await provisioning.EnsureProvisionedAsync(query.RequestId, RecordType.Request, cancellationToken);

        var entities = await context.RequestAgents
            .Where(a => a.RequestId == query.RequestId)
            .OrderBy(a => a.AssignedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(e => e.ToModel(registry.Find(e.AgentKey))).ToList().AsReadOnly();
    }
}
