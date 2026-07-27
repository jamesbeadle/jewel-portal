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

/// <summary>
/// A task the user has under way in a dialog open beside the chat — the assistant and the user
/// working on one document together rather than talking about it.
///
/// <para><see cref="DraftJson"/> is the dialog's field values AS THEY STAND RIGHT NOW, sent with
/// every turn. It is rendered into the system prompt rather than the transcript, deliberately: the
/// prompt is rebuilt from scratch each turn and never persisted, so the model always sees the
/// current values and never accumulates a pile of stale ones to disagree with.</para>
///
/// <para>Client-supplied and therefore untrusted. The server re-checks the dialog against the
/// caller's real roles before it renders any of this, and the contents reach the prompt clearly
/// labelled as data on the user's own screen — never as instructions, and never as a tool
/// argument.</para>
/// </summary>
public sealed record AiTaskScope(
    /// <summary>Also the conversation's CapabilityKey, e.g. "variation-draft".</summary>
    string TaskKey,
    /// <summary>A ModalCatalog key, e.g. "variation_draft".</summary>
    string ModalKey,
    string? RecordType,
    string? RecordId,
    /// <summary>What the user reads the record as — "RFI-049". What the model should say out loud.</summary>
    string? RecordReference,
    string? DraftJson);

/// <summary>Where the user is when they send a message. Assembled by the client from the route.</summary>
public sealed record AiScope(
    string? ProjectId,
    string? Route,
    string? PageLabel,
    /// <summary>Defaulted, so the plain three-argument construction sites keep working.</summary>
    AiTaskScope? Task = null);

public sealed record AiTurnResult(
    string ConversationId,
    AiTurnStatus Status,
    /// <summary>Only the messages produced by this turn — the client appends them.</summary>
    IReadOnlyList<AiChatMessage> NewMessages,
    IReadOnlyList<AiUiAction> UiActions,
    /// <summary>Tool names called, in order. Rendered in the panel so the work is visible.</summary>
    IReadOnlyList<string> ToolsUsed);
