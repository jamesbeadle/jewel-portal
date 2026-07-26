using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Ai;

/// <summary>
/// One user turn. The server holds the conversation — the client sends the new message and the scope
/// it was sent from, never the history. <c>SentByEmail</c> is re-stamped from the session.
///
/// <para>A null <c>ConversationId</c> starts a new conversation and the result carries the new id.</para>
/// </summary>
public sealed record SendAiMessage(
    string? ConversationId,
    string Message,
    AiScope Scope,
    string SentByEmail) : ICommand<AiTurnResult>;
