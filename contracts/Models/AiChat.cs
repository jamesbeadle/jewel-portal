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

public enum AiTurnStatus
{
    Complete = 0,
    /// <summary>The step budget or time budget ran out before the model finished. The reply is
    /// what it had; the user can ask it to carry on.</summary>
    Truncated = 1,
    /// <summary>No Anthropic key is configured. The panel says so rather than failing silently.</summary>
    Unavailable = 2
}

/// <summary>Where the user is when they send a message. Assembled by the client from the route.</summary>
public sealed record AiScope(
    string? ProjectId,
    string? Route,
    string? PageLabel);

public sealed record AiTurnResult(
    string ConversationId,
    AiTurnStatus Status,
    /// <summary>Only the messages produced by this turn — the client appends them.</summary>
    IReadOnlyList<AiChatMessage> NewMessages,
    IReadOnlyList<AiUiAction> UiActions,
    /// <summary>Tool names called, in order. Rendered in the panel so the work is visible.</summary>
    IReadOnlyList<string> ToolsUsed);
