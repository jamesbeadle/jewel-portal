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

        commands.Register<SendAiMessage, AiTurnResult>(
            CommandRoute.Post("/api/ai/messages"));
    }
}
