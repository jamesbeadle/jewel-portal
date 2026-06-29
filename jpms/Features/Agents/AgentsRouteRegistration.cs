using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Agents;

public static class AgentsRouteRegistration
{
    public static IServiceCollection AddAgentsReadModels(this IServiceCollection services)
    {
        services.AddScoped<AgentsReadModel>();
        return services;
    }

    public static void RegisterAgentsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListAvailableAgents, IReadOnlyList<AgentDescriptor>>(
            new QueryRoute("/api/agents", _ => "/api/agents"));

        queries.Register<ListRequestAgents, IReadOnlyList<RequestAgent>>(
            new QueryRoute("/api/requests/{requestId}/agents",
                query => $"/api/requests/{((ListRequestAgents)query).RequestId}/agents"));

        queries.Register<ListAgentChat, IReadOnlyList<AgentChatMessage>>(
            new QueryRoute("/api/requests/{requestId}/agents/{agentKey}/chat",
                query =>
                {
                    var q = (ListAgentChat)query;
                    return $"/api/requests/{q.RequestId}/agents/{q.AgentKey}/chat";
                }));

        queries.Register<ListAgentProposals, IReadOnlyList<AgentProposal>>(
            new QueryRoute("/api/requests/{requestId}/agent-proposals",
                query => $"/api/requests/{((ListAgentProposals)query).RequestId}/agent-proposals"));

        queries.Register<ListAgentQueue, IReadOnlyList<AgentQueueItem>>(
            new QueryRoute("/api/agent-queue", _ => "/api/agent-queue"));

        commands.Register<AssignAgent, RequestAgent>(
            new CommandRoute("POST", "/api/requests/{requestId}/agents",
                command => $"/api/requests/{((AssignAgent)command).RequestId}/agents"));

        commands.Register<RemoveRequestAgent, Acknowledgement>(
            new CommandRoute("DELETE", "/api/requests/{requestId}/agents/{requestAgentId}",
                command =>
                {
                    var c = (RemoveRequestAgent)command;
                    return $"/api/requests/{c.RequestId}/agents/{c.RequestAgentId}";
                }));

        commands.Register<SendAgentMessage, AgentChatMessage>(
            new CommandRoute("POST", "/api/requests/{requestId}/agents/{agentKey}/messages",
                command =>
                {
                    var c = (SendAgentMessage)command;
                    return $"/api/requests/{c.RequestId}/agents/{c.AgentKey}/messages";
                }));

        commands.Register<RunAgentAnalysis, AgentProposal>(
            new CommandRoute("POST", "/api/requests/{requestId}/agents/{agentKey}/analyse",
                command =>
                {
                    var c = (RunAgentAnalysis)command;
                    return $"/api/requests/{c.RequestId}/agents/{c.AgentKey}/analyse";
                }));

        commands.Register<DecideAgentProposal, AgentProposal>(
            new CommandRoute("POST", "/api/agent-proposals/{proposalId}/decide",
                command => $"/api/agent-proposals/{((DecideAgentProposal)command).ProposalId}/decide"));

        commands.Register<AttemptCloseRequest, RequestCloseOutcome>(
            new CommandRoute("POST", "/api/requests/{requestId}/agents/close",
                command => $"/api/requests/{((AttemptCloseRequest)command).RequestId}/agents/close"));
    }
}
