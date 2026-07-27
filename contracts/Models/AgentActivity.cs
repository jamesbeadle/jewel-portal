namespace Jewel.JPMS.Models;

/// <summary>What set the agent off. Pinned values — these persist as ints.</summary>
public enum AgentTrigger
{
    /// <summary>A person typed something in the chat panel.</summary>
    Chat = 0,
    /// <summary>A timer. Nobody was watching.</summary>
    Schedule = 1,
    /// <summary>A queue message — work handed off from elsewhere in the system.</summary>
    Queue = 2,
    /// <summary>A person pressed a button that runs an agent, outside the chat.</summary>
    Manual = 3
}

public enum AgentOutcome
{
    Ok = 0,
    /// <summary>The run errored. <c>Summary</c> says how.</summary>
    Failed = 1,
    /// <summary>The agent asked to do something it was not permitted to do.</summary>
    Refused = 2,
    /// <summary>An integration it needed is not configured — see the not-configured tool pattern.</summary>
    NotConfigured = 3,
    /// <summary>It ran out of steps or time before finishing.</summary>
    Truncated = 4
}

/// <summary>
/// One agent run. The unit is a run, not a message: a chat turn is one row, and a scheduled sweep
/// will be one row per record it acts on.
///
/// <para>This is the answer to "when did the machine act, on whose behalf, and what did it touch".
/// It is deliberately separate from <c>AuditEvent</c>, which records what <em>people</em> did to
/// client correspondence — different question, different columns.</para>
/// </summary>
public sealed record AgentActivity(
    string ActivityId,
    string AgentKey,
    AgentTrigger Trigger,

    /// <summary>Who it ran as. For a scheduled agent this is the system pseudo-user.</summary>
    string ActorEmail,
    /// <summary>True when no human was in the loop at the moment it ran.</summary>
    bool IsAutonomous,

    /// <summary>Dotted name for what it did: <c>chat.turn</c>, <c>sweep.request</c>, <c>draft.email</c>.</summary>
    string Action,
    AgentOutcome Outcome,
    string Summary,

    string? ConversationId,
    string? ProjectId,
    string? RecordReference,
    /// <summary>Where to go to see what it touched.</summary>
    string? Route,

    /// <summary>Tools called, in order, comma-separated. Empty for a run that called none.</summary>
    string? ToolsUsed,

    int DurationMs,
    int InputTokens,
    int OutputTokens,
    /// <summary>Cost in pence, computed at write time so a later rate change never rewrites history.</summary>
    decimal CostPence,

    DateTimeOffset OccurredAt)
{
    public int TotalTokens => InputTokens + OutputTokens;

    public string CostDisplay => CostPence >= 100m
        ? $"£{CostPence / 100m:0.00}"
        : $"{CostPence:0.0}p";
}
