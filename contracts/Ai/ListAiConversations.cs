using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Ai;

/// <summary>
/// The caller's past assistant conversations, newest first — the panel's history list. The server
/// scopes the answer to the signed-in user; the record deliberately carries nothing that could
/// widen it (a conversation id is not a capability, and neither is a list of them).
/// </summary>
public sealed record ListAiConversations(int Take = 30) : IQuery<IReadOnlyList<AiConversationSummary>>;
