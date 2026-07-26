using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Ai;

/// <summary>Replays a conversation — user and assistant turns only, tool results excluded.</summary>
public sealed record ListAiConversation(string ConversationId) : IQuery<IReadOnlyList<AiChatMessage>>;
