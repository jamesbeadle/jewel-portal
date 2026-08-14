namespace Jewel.JPMS.Models;

/// <summary>Pinned values — these persist as ints.</summary>
public enum AiChatRole
{
    User = 0,
    Assistant = 1,
    /// <summary>A tool result. Kept in the transcript for audit and for the model's next turn, but
    /// not rendered as a bubble.</summary>
    Tool = 2,
    /// <summary>Context carried over from the user's previous conversation when a task started a
    /// fresh one. Replayed to the model as background, never rendered as a bubble — it is
    /// continuity, not conversation.</summary>
    Context = 3
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

/// <summary>
/// The task the user and the assistant are doing together in a dialog beside the chat — the client
/// side of this is <c>AiTaskState</c> / <c>AiTask</c>, and <see cref="ModalKey"/> names a
/// <c>ModalCatalog</c> entry.
///
/// <para><see cref="DraftJson"/> is the dialog's field values as they stand right now, sent with
/// every turn so the model always sees the user's own edits. It crosses as JSON on purpose: the page
/// that owns the dialog owns its shape, and the moment this record knows what a variation looks like
/// the mechanism stops being reusable.</para>
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
    /// <summary>
    /// The routes this user can reach, compact, built client-side from NavigationCatalog so it is
    /// role-correct by construction and cannot drift from the real sidebar. Sent by the client
    /// because the catalogue lives in the Blazor project — a route list is not security-relevant
    /// (every page and endpoint gates itself), so client-supplied is acceptable here and nowhere else.
    /// </summary>
    string? SiteMap = null,
    /// <summary>The kind of record the route is showing — "variation", "request". Null off a record page.</summary>
    string? RecordType = null,
    string? RecordId = null,
    /// <summary>Set when a task dialog is open beside the chat. Null for an ordinary conversation.</summary>
    AiTaskScope? Task = null);

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
    int StepsRemaining,
    /// <summary>The agent in force AFTER this hop (an AgentCatalogue key) — a switch_agent call
    /// takes effect on the next hop, and the panel shows this so a change of hat is visible.
    /// Defaulted so pre-agent clients and cached responses keep deserialising.</summary>
    string? AgentKey = null)
{
    /// <summary>The label to show beside the pulsing jewel while the next hop runs.</summary>
    public string? LatestLabel => Steps.Count > 0 ? Steps[^1].Label : null;
}
