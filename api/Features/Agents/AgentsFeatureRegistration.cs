using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Agents.Commands;
using Jewel.JPMS.Api.Features.Agents.Queries;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Agents;

public static class AgentsFeatureRegistration
{
    public static IServiceCollection AddAgentsFeature(this IServiceCollection services)
    {
        // The agents themselves. Each is a singleton; the registry collects them all. Adding a new
        // agent is a single line here — its key, chat, analysis and close-gate wiring follow.
        services.AddSingleton<IRequestAgent, BidPackagesAgent>();
        services.AddSingleton<IRequestAgent, SchedulingAgent>();
        services.AddSingleton<IRequestAgent, ValuationsAgent>();
        services.AddSingleton<AgentRegistry>();

        services.AddScoped<RequestContextAssembler>();

        // Queries
        services.AddScoped<IQueryHandler<ListAvailableAgents, IReadOnlyList<AgentDescriptor>>, ListAvailableAgentsHandler>();
        services.AddScoped<IQueryHandler<ListRequestAgents, IReadOnlyList<RequestAgent>>, ListRequestAgentsHandler>();
        services.AddScoped<IQueryHandler<ListAgentChat, IReadOnlyList<AgentChatMessage>>, ListAgentChatHandler>();
        services.AddScoped<IQueryHandler<ListAgentProposals, IReadOnlyList<AgentProposal>>, ListAgentProposalsHandler>();
        services.AddScoped<IQueryHandler<ListAgentQueue, IReadOnlyList<AgentQueueItem>>, ListAgentQueueHandler>();

        // Commands
        services.AddScoped<ICommandHandler<AssignAgent, RequestAgent>, AssignAgentHandler>();
        services.AddScoped<AssignAgentAuthorisation>();
        services.AddScoped<AssignAgentValidation>();

        services.AddScoped<ICommandHandler<RemoveRequestAgent, Acknowledgement>, RemoveRequestAgentHandler>();
        services.AddScoped<RemoveRequestAgentAuthorisation>();
        services.AddScoped<RemoveRequestAgentValidation>();

        services.AddScoped<ICommandHandler<SendAgentMessage, AgentChatMessage>, SendAgentMessageHandler>();
        services.AddScoped<SendAgentMessageAuthorisation>();
        services.AddScoped<SendAgentMessageValidation>();

        services.AddScoped<ICommandHandler<RunAgentAnalysis, AgentProposal>, RunAgentAnalysisHandler>();
        services.AddScoped<RunAgentAnalysisAuthorisation>();
        services.AddScoped<RunAgentAnalysisValidation>();

        services.AddScoped<ICommandHandler<DecideAgentProposal, AgentProposal>, DecideAgentProposalHandler>();
        services.AddScoped<DecideAgentProposalAuthorisation>();
        services.AddScoped<DecideAgentProposalValidation>();

        services.AddScoped<ICommandHandler<AttemptCloseRequest, RequestCloseOutcome>, AttemptCloseRequestHandler>();
        services.AddScoped<AttemptCloseRequestAuthorisation>();
        services.AddScoped<AttemptCloseRequestValidation>();

        return services;
    }
}
