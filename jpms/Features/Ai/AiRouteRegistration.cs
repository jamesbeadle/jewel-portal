using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Cqrs;
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

        // The panel's history list — the caller's own past conversations, newest first.
        queries.Register<ListAiConversations, IReadOnlyList<AiConversationSummary>>(
            new QueryRoute("/api/ai/conversations",
                query => $"/api/ai/conversations?take={((ListAiConversations)query).Take}"));

        queries.Register<ListAgentActivity, IReadOnlyList<AgentActivity>>(
            new QueryRoute("/api/agents/activity", BuildAgentActivityPath));

        commands.Register<SendAiMessage, AiTurnResult>(
            CommandRoute.Post("/api/ai/messages"));

        commands.Register<ContinueAiTurn, AiTurnResult>(
            CommandRoute.Post("/api/ai/turn/continue"));

        // A reply that outlived its request's inline wait (docs/ai/07-reply-collection.md).
        commands.Register<CollectAiReply, AiTurnResult>(
            CommandRoute.Post("/api/ai/turn/collect"));

        commands.Register<AddAiAttachment, AiAttachmentReceipt>(
            CommandRoute.Post("/api/ai/attachments"));

        // The skill store — the AI Skills admin page (docs/ai/05-agents-and-skills.md §2).
        queries.Register<ListAiSkills, IReadOnlyList<SkillSummary>>(
            new QueryRoute("/api/ai/skills", _ => "/api/ai/skills"));

        queries.Register<GetAiSkill, SkillDetail?>(
            new QueryRoute("/api/ai/skills/{skillKey}",
                query => $"/api/ai/skills/{Uri.EscapeDataString(((GetAiSkill)query).SkillKey)}"));

        commands.Register<SaveAiSkill, Acknowledgement>(
            CommandRoute.Post("/api/ai/skills"));

        commands.Register<SaveAiSkillReference, Acknowledgement>(
            CommandRoute.Post("/api/ai/skills/references"));
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
