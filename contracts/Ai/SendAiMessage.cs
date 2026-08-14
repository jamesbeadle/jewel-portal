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
    string SentByEmail,
    /// <summary>When starting a NEW conversation (a task kick-off), the conversation the user was
    /// in just before. The server carries its tail over as a Context row so the assistant remembers
    /// what was being discussed. Ignored when continuing an existing conversation, and silently
    /// dropped when the previous conversation belongs to someone else.</summary>
    string? PreviousConversationId = null,
    /// <summary>An <see cref="AiModelCatalogue"/> key — the model the user chose in the panel.
    /// Null or unknown degrades to the cheap default server-side; the client can never name a raw
    /// model id.</summary>
    string? Model = null) : ICommand<AiTurnResult>;
