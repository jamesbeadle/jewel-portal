using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Ai;

/// <summary>
/// Collects a reply that outlived its request's inline wait (docs/ai/07-reply-collection.md).
/// Sent by the panel when a hop answered <see cref="AiTurnStatus.Pending"/>, and again for as
/// long as the collect itself answers pending. The result is the finished hop — the same shape
/// the hop would have returned had Claude answered inside the wait. <c>SentByEmail</c> is
/// re-stamped from the session, and the handler refuses a conversation the caller did not start.
/// </summary>
public sealed record CollectAiReply(
    string ConversationId,
    /// <summary>The <see cref="AiTurnResult.PendingReplyId"/> the pending hop returned.</summary>
    string ReplyId,
    AiScope Scope,
    string SentByEmail) : ICommand<AiTurnResult>;
