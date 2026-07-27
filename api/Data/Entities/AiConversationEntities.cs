using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One assistant conversation. The server is authoritative: the transcript is assembled from these
/// rows on every turn, never from anything the client sends. That is what makes it usable as a
/// record of how a draft or a decision came about.
/// </summary>
public sealed class AiConversationEntity
{
    [Key, MaxLength(64)] public string ConversationId { get; set; } = "";
    [MaxLength(64)] public string? ProjectId { get; set; }
    [MaxLength(512)] public string? Route { get; set; }
    [MaxLength(64)] public string CapabilityKey { get; set; } = "orchestrator";

    /// <summary>
    /// The record a task conversation was about — <c>RecordType.Request</c> and that request's id
    /// for a variation draft. Both null for a general conversation, which is scoped to a page rather
    /// than to a record.
    ///
    /// <para>Stored as loose strings, like every other cross-feature link in this schema (there are
    /// no foreign keys). Their job is to make "show me the exchange that drafted V72" one query when
    /// somebody asks in two years' time — docs/ai/00-agent-architecture.md §8.</para>
    /// </summary>
    [MaxLength(64)] public string? ScopeRecordType { get; set; }
    [MaxLength(64)] public string? ScopeRecordId { get; set; }

    [MaxLength(256)] public string StartedByEmail { get; set; } = "";
    [MaxLength(256)] public string? Title { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset LastMessageAt { get; set; }
}

/// <summary>
/// One message. <see cref="Body"/> is unbounded on purpose — a tool result carrying a variation
/// register would be silently truncated by a MaxLength, and a silently truncated tool result is a
/// model reasoning from partial data without knowing it.
/// </summary>
public sealed class AiConversationMessageEntity
{
    [Key, MaxLength(64)] public string MessageId { get; set; } = "";
    [MaxLength(64)] public string ConversationId { get; set; } = "";
    public int Role { get; set; }
    public string Body { get; set; } = "";
    /// <summary>Set on Tool rows; also set on Assistant rows that asked for a tool.</summary>
    [MaxLength(128)] public string? ToolName { get; set; }
    /// <summary>Anthropic's tool_use id, so a result can be paired back to its call.</summary>
    [MaxLength(128)] public string? ToolUseId { get; set; }
    /// <summary>Monotonic within a conversation. Ordering by timestamp is not safe — several rows
    /// are written inside one turn and can share a millisecond.</summary>
    public int Sequence { get; set; }
    public DateTimeOffset PostedAt { get; set; }
}
