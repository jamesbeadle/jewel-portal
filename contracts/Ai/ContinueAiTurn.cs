using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Ai;

/// <summary>
/// The next hop of a turn already in flight. Carries no message — everything the model needs is
/// already in the transcript, server-side. <c>SentByEmail</c> is re-stamped from the session, and the
/// handler refuses a conversation the caller did not start.
/// </summary>
public sealed record ContinueAiTurn(
    string ConversationId,
    AiScope Scope,
    string SentByEmail) : ICommand<AiTurnResult>;
