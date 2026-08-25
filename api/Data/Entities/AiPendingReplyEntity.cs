using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One Claude call in flight for a conversation — the answer to "ask, then collect"
/// (docs/ai/07-reply-collection.md). The Static Web Apps gateway cuts any request at ~45s and a
/// capable model on a cold cache does not always answer in that; so the call runs on a background
/// task with its own budget and the request that started it, or a later collect, reads the answer
/// from this row. The row is what lets a collect served by a different instance find the answer.
/// </summary>
public sealed class AiPendingReplyEntity
{
    [Key, MaxLength(64)] public string ReplyId { get; set; } = "";
    [MaxLength(64)] public string ConversationId { get; set; } = "";
    /// <summary>The transcript's highest sequence when the call went out. A collect whose
    /// transcript has moved past this refuses — a late answer is never spliced into a transcript
    /// that no longer matches the prompt it was for.</summary>
    public int AfterSequence { get; set; }
    /// <summary>The AiModelCatalogue key the hop was asked for, so a collect logs what ran.</summary>
    [MaxLength(32)] public string? ModelTier { get; set; }
    /// <summary>Checked on write: two collects racing for the same answer become a concurrency
    /// failure for the second, never a double-applied hop.</summary>
    [ConcurrencyCheck] public AiPendingReplyStatus Status { get; set; }
    /// <summary>The ClaudeReply as JSON once answered; null while in flight or on failure.</summary>
    public string? ReplyJson { get; set; }
    /// <summary>The failure class (timeout, busy, connection …) when the call failed.</summary>
    [MaxLength(64)] public string? Error { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? AnsweredAt { get; set; }
}

public enum AiPendingReplyStatus
{
    /// <summary>The background call is running (or the instance running it has died — a collect
    /// older than the deadline treats it so).</summary>
    InFlight = 0,
    /// <summary>Claude answered; <see cref="AiPendingReplyEntity.ReplyJson"/> holds the reply and
    /// no hop has applied it yet.</summary>
    Answered = 1,
    /// <summary>The call failed; <see cref="AiPendingReplyEntity.Error"/> says how.</summary>
    Failed = 2,
    /// <summary>A hop applied the answer (or refused it because the transcript moved on).</summary>
    Consumed = 3
}
