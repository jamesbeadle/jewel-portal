using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Ai;

public static class AiRouteRegistration
{
    public static void RegisterAiRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListAiConversation, IReadOnlyList<AiChatMessage>>(
            new QueryRoute("/api/ai/conversations/{conversationId}",
                query => $"/api/ai/conversations/{((ListAiConversation)query).ConversationId}"));

        queries.Register<ListAgentActivity, IReadOnlyList<AgentActivity>>(
            new QueryRoute("/api/agents/activity", BuildAgentActivityPath));

        commands.Register<SendAiMessage, AiTurnResult>(
            CommandRoute.Post("/api/ai/messages"));
    }

    private static string BuildAgentActivityPath(object query)
    {
        var request = (ListAgentActivity)query;
        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.ProjectId))
            parameters.Add($"projectId={Uri.EscapeDataString(request.ProjectId)}");
        if (!string.IsNullOrWhiteSpace(request.AgentKey))
            parameters.Add($"agentKey={Uri.EscapeDataString(request.AgentKey)}");
        if (request.AutonomousOnly == true)
            parameters.Add("autonomousOnly=1");
        if (request.Take > 0)
            parameters.Add($"take={request.Take}");

        return parameters.Count == 0
            ? "/api/agents/activity"
            : $"/api/agents/activity?{string.Join("&", parameters)}";
    }
}
