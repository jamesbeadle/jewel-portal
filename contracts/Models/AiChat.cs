namespace Jewel.JPMS.Models;

/// <summary>Pinned values — these persist as ints.</summary>
public enum AiChatRole
{
    User = 0,
    Assistant = 1,
    /// <summary>A tool result. Kept in the transcript for audit and for the model's next turn, but
    /// not rendered as a bubble.</summary>
    Tool = 2
}

public sealed record AiChatMessage(
    string MessageId,
    AiChatRole Role,
    string Body,
    string? ToolName,
    DateTimeOffset PostedAt);

/// <summary>
/// What the assistant asked the browser to do. Executed client-side; nothing security-relevant is
/// asserted by the client, so the result is advisory.
/// </summary>
public sealed record AiUiAction(string Tool, string ArgumentsJson);

/// <summary>
/// One thing the assistant did during a hop, phrased for a human watching the panel.
///
/// <para>This is what replaces streaming. We do not get a token stream from the API, but we always
/// know which tool we are about to call and why — so the panel says "Looking up V72" rather than
/// "Thinking…" for twenty seconds.</para>
/// </summary>
public sealed record AiStep(string Label, string Tool, bool Ok);

public enum AiTurnStatus
{
    /// <summary>The assistant has finished. Nothing more to send.</summary>
    Complete = 0,
    /// <summary>The step budget or time budget ran out. The reply is what it had.</summary>
    Truncated = 1,
    /// <summary>No Anthropic key, or the API could not be reached.</summary>
    Unavailable = 2,
    /// <summary>Tools ran and the model needs another hop. The client calls continue.</summary>
    NeedsContinue = 3
}

/// <summary>Where the user is when they send a message. Assembled by the client from the route.</summary>
public sealed record AiScope(
    string? ProjectId,
    string? Route,
    string? PageLabel);

/// <summary>
/// One hop, not one turn. A turn is a sequence of hops the client pumps until
/// <see cref="Status"/> stops being <see cref="AiTurnStatus.NeedsContinue"/>.
/// </summary>
public sealed record AiTurnResult(
    string ConversationId,
    AiTurnStatus Status,
    /// <summary>Messages produced by this hop only — the client appends them.</summary>
    IReadOnlyList<AiChatMessage> NewMessages,
    IReadOnlyList<AiUiAction> UiActions,
    /// <summary>What happened in this hop, in order, for the live status line.</summary>
    IReadOnlyList<AiStep> Steps,
    /// <summary>Hops left in the budget. Zero means the next one will be the last.</summary>
    int StepsRemaining)
{
    /// <summary>The label to show beside the pulsing jewel while the next hop runs.</summary>
    public string? LatestLabel => Steps.Count > 0 ? Steps[^1].Label : null;
}
