using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Agents.Queries;

public sealed class ListAvailableAgentsHandler : IQueryHandler<ListAvailableAgents, IReadOnlyList<AgentDescriptor>>
{
    private readonly AgentRegistry registry;
    public ListAvailableAgentsHandler(AgentRegistry registry) { this.registry = registry; }

    public Task<IReadOnlyList<AgentDescriptor>> HandleAsync(ListAvailableAgents query, CancellationToken cancellationToken)
        => Task.FromResult(registry.Descriptors());
}
