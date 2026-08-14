using System.Diagnostics;
using System.Text.Json;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// One hop of a turn: rebuild the transcript, make exactly ONE Claude call, run whatever tools it
/// asked for, persist everything, and hand back what happened.
///
/// <para>Deliberately not a loop. The client pumps, so the panel can say "Looking up V72" while the
/// next hop runs instead of showing a spinner for twenty seconds. It also keeps every request far
/// inside the Static Web Apps gateway's ~45 seconds, which a four-call loop never reliably was.</para>
///
/// <para>The server stays authoritative throughout: the transcript is rebuilt from the database on
/// every hop and the client contributes nothing but its own user message.</para>
/// </summary>
public sealed class AiTurnRunner
{
    /// <summary>Claude round trips per user message, across all hops. Enough for a real multi-step
    /// job — look-ups, a navigation, a draft — and bounded so a confused model cannot spend
    /// indefinitely. Raised from 6 alongside prompt caching (which makes a hop cheap) and the
    /// budget line in the turn context (which lets the model plan its spend); the panel's "Carry
    /// on" chip is the escape hatch when even this is not enough.</summary>
    public const int MaxHops = 10;

    private readonly JpmsContext context;
    private readonly IClaudeConversationClient claude;
    private readonly AgentActivityLog activityLog;
    private readonly IServiceProvider services;

    public AiTurnRunner(
        JpmsContext context, IClaudeConversationClient claude,
        AgentActivityLog activityLog, IServiceProvider services)
    {
        this.context = context;
        this.claude = claude;
        this.activityLog = activityLog;
        this.services = services;
    }

    public async Task<AiTurnResult> RunHopAsync(
        AiConversationEntity conversation,
        SignedInUser user,
        AiScope? scope,
        string? modelTier,
        CancellationToken cancellationToken)
    {
        var clock = Stopwatch.StartNew();
        var rows = await LoadAsync(conversation.ConversationId, cancellationToken);
        var sequence = rows.Count == 0 ? 0 : rows.Max(row => row.Sequence);

        // Hops already spent on THIS user message — derived from the transcript rather than stored,
        // so there is no mid-turn state that can be left stale by a failed request.
        var lastUserIndex = rows.FindLastIndex(row => row.Role == (int)AiChatRole.User);
        var hopsSpent = lastUserIndex < 0
            ? 0
            : rows.Skip(lastUserIndex).Count(row => row.Role == (int)AiChatRole.Assistant);

        var newMessages = new List<AiConversationMessageEntity>();
        var steps = new List<AiStep>();
        var uiActions = new List<AiUiAction>();

        if (!claude.IsConfigured)
        {
            var unavailable = Add(conversation, AiChatRole.Assistant,
                "The assistant is not connected yet — no Anthropic API key is configured on this "
                + "environment. Ask an administrator to set Anthropic__ApiKey.",
                ++sequence, newMessages);
            await SaveAsync(conversation, cancellationToken);
            await LogAsync(conversation, user, scope, AgentOutcome.NotConfigured,
                "No Anthropic API key is configured.", steps, clock, 0, 0, cancellationToken);
            return Result(conversation, AiTurnStatus.Unavailable, newMessages, uiActions, steps, 0);
        }

        if (hopsSpent >= MaxHops)
        {
            var stopped = Add(conversation, AiChatRole.Assistant,
                "I've used up the look-ups I'm allowed for one question. Ask me again and I'll carry on "
                + "from where I got to.",
                ++sequence, newMessages);
            await SaveAsync(conversation, cancellationToken);
            await LogAsync(conversation, user, scope, AgentOutcome.Truncated,
                "Hop budget exhausted.", steps, clock, 0, 0, cancellationToken);
            return Result(conversation, AiTurnStatus.Truncated, newMessages, uiActions, steps, 0);
        }

        var project = string.IsNullOrWhiteSpace(scope?.ProjectId)
            ? null
            : await context.Projects.AsNoTracking()
                .FirstOrDefaultAsync(row => row.ProjectId == scope!.ProjectId, cancellationToken);

        // The agent in force this hop. A conversation stamped with a key the catalogue no longer
        // knows (renamed, retired) degrades to the orchestrator rather than failing the turn; a
        // key the CALLER may not engage degrades the same way — a conversation id is not a
        // capability, and neither is a persisted agent key.
        var agent = AgentCatalogue.Find(conversation.CapabilityKey) ?? AgentCatalogue.Orchestrator;
        if (!AgentCatalogue.CanEngage(agent, user.Roles)) agent = AgentCatalogue.Orchestrator;

        // The agent's skills plus the shared set, fresh from the database every hop — a portal
        // edit is in force on the very next message. Bodies are fetched for PINNED skills only;
        // the rest ride as one menu line each and arrive via load_skill.
        var skills = await LoadSkillsAsync(agent.Key, cancellationToken);

        var systemPrompt = AiSystemPrompt.Build(user, scope, project?.Reference, project?.Name, agent, skills);
        var tools = AiToolCatalogue.For(user, scope, agent)
            .Select(tool => new ClaudeToolSpec(tool.Name, tool.Description, tool.InputSchema))
            .ToList();

        // The volatile facts — where the user is, the dialog's live contents, the look-up budget —
        // ride as a block on the NEWEST message rather than in the system prompt, so the system
        // prompt and the transcript prefix stay byte-stable across hops and cache (see
        // ClaudeConversationClient). Rebuilt every hop, never persisted.
        var turnContext = AiSystemPrompt.BuildTurnContext(
            user, scope, project?.Reference, project?.Name, hopsSpent, MaxHops);

        var reply = await claude.ContinueAsync(
            systemPrompt, BuildTranscript(rows, turnContext), tools, modelTier, cancellationToken);

        if (!reply.Ok)
        {
            var failed = Add(conversation, AiChatRole.Assistant,
                "I could not reach the Claude API just then. Try again in a moment — nothing has been changed.",
                ++sequence, newMessages);
            await SaveAsync(conversation, cancellationToken);
            await LogAsync(conversation, user, scope, AgentOutcome.Failed,
                $"The Claude API call failed ({reply.Error}).", steps, clock, 0, 0, cancellationToken);
            return Result(conversation, AiTurnStatus.Unavailable, newMessages, uiActions, steps, 0);
        }

        // The assistant turn is persisted WITH its tool_use blocks. Without them the tool_result that
        // follows has nothing to pair with and Anthropic rejects the next hop.
        var assistantRow = Add(conversation, AiChatRole.Assistant, reply.Text ?? "", ++sequence, newMessages);
        if (reply.ToolCalls.Count > 0)
        {
            assistantRow.ToolCallsJson = JsonSerializer.Serialize(
                reply.ToolCalls.Select(call => new { id = call.Id, name = call.Name, input = call.ArgumentsJson }));
        }

        var toolContext = new AiToolContext(context, user, scope, services, agent.Key);

        foreach (var call in reply.ToolCalls)
        {
            var label = AiToolLabels.For(call.Name, call.ArgumentsJson);
            var tool = AiToolCatalogue.Find(call.Name);
            string output;
            var ok = true;

            if (tool is null)
            {
                output = Fail($"No tool named {call.Name}.");
                ok = false;
            }
            else if (!tool.VisibleTo.IncludesAny(user.Roles))
            {
                output = Fail("You are not permitted to use that tool.");
                ok = false;
            }
            else if (string.Equals(call.Name, AiToolCatalogue.SwitchAgent, StringComparison.OrdinalIgnoreCase))
            {
                // Executed here, not in the catalogue: it writes the conversation's own
                // CapabilityKey, which no ordinary tool can reach. Takes effect on the NEXT hop —
                // this hop's tools and prompt were assembled under the old agent, and rebuilding
                // them mid-hop would mean a tool list the model was never shown.
                output = SwitchAgent(conversation, user, call.ArgumentsJson, out ok);
            }
            else if (tool.Kind == AiToolKind.Ui)
            {
                // Checked BEFORE handing to the browser. A Ui tool's "ok" used to mean only
                // "posted" — so an open_modal with an invented record id navigated the user to a
                // dead page while the model narrated success. Refusing here puts the failure in
                // front of the model instead, and it corrects course.
                var refusal = await ValidateUiActionAsync(call, cancellationToken);
                if (refusal is not null)
                {
                    output = refusal;
                    ok = false;
                }
                else
                {
                    uiActions.Add(new AiUiAction(call.Name, call.ArgumentsJson));
                    output = JsonSerializer.Serialize(new { ok = true, handed_to_browser = true });
                }
            }
            else
            {
                try
                {
                    using var arguments = JsonDocument.Parse(
                        string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson);
                    output = await tool.ExecuteAsync(toolContext, arguments.RootElement, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    output = Fail($"The tool failed: {ex.Message}");
                    ok = false;
                }
            }

            var toolRow = Add(conversation, AiChatRole.Tool, output, ++sequence, newMessages);
            toolRow.ToolName = call.Name;
            toolRow.ToolUseId = call.Id;

            steps.Add(new AiStep(label, call.Name, ok));
        }

        await SaveAsync(conversation, cancellationToken);

        var moreToDo = reply.ToolCalls.Count > 0;
        var remaining = Math.Max(0, MaxHops - (hopsSpent + 1));
        var status = !moreToDo ? AiTurnStatus.Complete
            : remaining == 0 ? AiTurnStatus.Truncated
            : AiTurnStatus.NeedsContinue;

        // One activity row per hop. A turn that took three hops reads as three rows, which is what
        // you want when reconstructing what the assistant actually did.
        await LogAsync(conversation, user, scope,
            status == AiTurnStatus.Truncated ? AgentOutcome.Truncated : AgentOutcome.Ok,
            string.IsNullOrWhiteSpace(reply.Text)
                ? string.Join(", ", steps.Select(step => step.Label))
                : Truncate(reply.Text!, 400),
            steps, clock, reply.InputTokens, reply.OutputTokens, cancellationToken);

        return Result(conversation, status, newMessages, uiActions, steps, remaining, reply.EscalationNote);
    }

    // ---- Ui-action validation ----------------------------------------------------------------

    /// <summary>
    /// Sanity-checks a Ui action the server can verify, returning a refusal payload (or null to
    /// proceed). Today that is open_modal: the dialog must exist, a dialog whose route names a
    /// record must be given a record id, and for variation_draft that id must be a REAL request —
    /// the failure the model must see is "no such request", not a user stranded on a dead page.
    /// </summary>
    private async Task<string?> ValidateUiActionAsync(ClaudeToolCall call, CancellationToken ct)
    {
        if (!string.Equals(call.Name, "open_modal", StringComparison.OrdinalIgnoreCase)) return null;

        string? modalKey = null;
        string? recordId = null;
        try
        {
            using var arguments = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson);
            if (arguments.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (arguments.RootElement.TryGetProperty("modal_key", out var keyElement)
                    && keyElement.ValueKind == JsonValueKind.String)
                    modalKey = keyElement.GetString();
                if (arguments.RootElement.TryGetProperty("record_id", out var recordElement)
                    && recordElement.ValueKind == JsonValueKind.String)
                    recordId = recordElement.GetString();
            }
        }
        catch (JsonException)
        {
        }

        var modal = ModalCatalog.Find(modalKey);
        if (modal is null)
        {
            return Fail($"No dialog named {modalKey}. The dialogs are: "
                + string.Join(", ", ModalCatalog.All.Select(candidate => candidate.ModalKey)) + ".");
        }

        var needsRecord = modal.RouteTemplate.Contains("{record}", StringComparison.Ordinal);
        if (!needsRecord) return null;

        if (string.IsNullOrWhiteSpace(recordId))
        {
            return Fail($"{modal.ModalKey} needs record_id — the request id from find_by_reference or "
                + "list_requests. Do not invent one. For a variation with no RFI behind it, use "
                + "manual_variation instead.");
        }

        // variation_draft works from a request; verify it actually exists before anyone navigates.
        var exists = await context.Requests.AsNoTracking()
            .AnyAsync(row => row.RequestId == recordId, ct);
        if (!exists)
        {
            return Fail($"No request exists with id \"{recordId}\" — that is not a real record id. Call "
                + "find_by_reference or list_requests for the actual id. For a variation with no RFI "
                + "behind it, use manual_variation instead.");
        }

        return null;
    }

    // ---- agents and skills -----------------------------------------------------------------

    /// <summary>
    /// The model asked to change hat. Validated exactly like any other capability: the key must
    /// exist and the CALLER's roles must be allowed to engage it — the model cannot talk its way
    /// into an agent the person could not have chosen from a menu.
    /// </summary>
    private string SwitchAgent(
        AiConversationEntity conversation, SignedInUser user, string? argumentsJson, out bool ok)
    {
        ok = false;

        string? key = null;
        try
        {
            using var arguments = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            if (arguments.RootElement.ValueKind == JsonValueKind.Object
                && arguments.RootElement.TryGetProperty("agent", out var value)
                && value.ValueKind == JsonValueKind.String)
            {
                key = value.GetString();
            }
        }
        catch (JsonException)
        {
        }

        var destination = AgentCatalogue.Find(key);
        if (destination is null)
            return Fail($"No agent named {key}. Only the agents listed in the tool description exist.");
        if (!AgentCatalogue.CanEngage(destination, user.Roles))
            return Fail("This user may not engage that agent.");
        if (string.Equals(conversation.CapabilityKey, destination.Key, StringComparison.OrdinalIgnoreCase))
            return Fail($"The {destination.DisplayName} agent is already in force.");

        conversation.CapabilityKey = destination.Key;
        ok = true;
        return JsonSerializer.Serialize(new
        {
            ok = true,
            switched_to = destination.Key,
            note = $"You are now the {destination.DisplayName} agent. Your tools, working rules and "
                   + "skills change from your next step — continue the task under them."
        });
    }

    /// <summary>The agent's skills plus the shared set. Pinned bodies come back whole; unpinned
    /// skills come back as menu lines (key + description) with no body — load_skill fetches those.</summary>
    private async Task<IReadOnlyList<AiSystemPrompt.PromptSkill>> LoadSkillsAsync(
        string agentKey, CancellationToken ct)
    {
        var rows = await context.Skills
            .AsNoTracking()
            .Where(row => row.IsActive
                          && (row.AgentKey == agentKey || row.AgentKey == AiSkillTools.SharedAgentKey))
            .OrderBy(row => row.AgentKey == AiSkillTools.SharedAgentKey ? 0 : 1)
            .ThenBy(row => row.DisplayName)
            .Select(row => new
            {
                row.SkillKey, row.DisplayName, row.Description, row.Pinned, row.Version,
                Body = row.Pinned ? row.Body : null
            })
            .ToListAsync(ct);

        return rows
            .Select(row => new AiSystemPrompt.PromptSkill(
                row.SkillKey, row.DisplayName, row.Description, row.Pinned, row.Version, row.Body))
            .ToList();
    }

    // ---- transcript ------------------------------------------------------------------------

    private Task<List<AiConversationMessageEntity>> LoadAsync(string conversationId, CancellationToken ct) =>
        context.AiConversationMessages
            .AsNoTracking()
            .Where(row => row.ConversationId == conversationId)
            .OrderBy(row => row.Sequence)
            .ToListAsync(ct);

    /// <summary>
    /// Rebuilds the Anthropic messages array. Assistant rows carrying tool_use blocks are replayed
    /// verbatim, and the tool rows that follow are grouped into a single user message of
    /// tool_result blocks — the shape the API requires.
    /// </summary>
    /// <summary>
    /// Rebuilds the messages array WITH the two cost bounds applied first: repeated identical
    /// calls to the big tools replay latest-only, and the whole transcript is stubbed
    /// oldest-tool-first down to <see cref="AiTranscriptBudget.MaxTranscriptChars"/>. Both bounds
    /// touch tool-result bodies only — a user or assistant turn is the conversation itself and is
    /// never rewritten. The logic lives in contracts (AiTranscriptBudget) so the test project can
    /// pin it; this method's job is to feed it and to keep the tool_use/tool_result pairing the
    /// API requires.
    /// </summary>
    private static List<object> BuildTranscript(List<AiConversationMessageEntity> rows, string turnContext)
    {
        // ---- the budget pass -----------------------------------------------------------------
        var bodies = new string[rows.Count];
        for (var i = 0; i < rows.Count; i++) bodies[i] = rows[i].Body ?? "";

        // A tool row's identity for the supersede rule is name + the arguments that produced it,
        // and the arguments live on the assistant row's stored tool_use blocks — pair them back up.
        var argumentsByToolUseId = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if ((AiChatRole)row.Role != AiChatRole.Assistant) continue;
            foreach (var call in ReadToolCalls(row.ToolCallsJson))
                argumentsByToolUseId[call.Id!] = call.Input ?? "";
        }

        var toolRows = new List<TranscriptToolRow>();
        for (var i = 0; i < rows.Count; i++)
        {
            if ((AiChatRole)rows[i].Role != AiChatRole.Tool) continue;
            var arguments = rows[i].ToolUseId is { } id && argumentsByToolUseId.TryGetValue(id, out var input)
                ? input
                : null;
            toolRows.Add(new TranscriptToolRow(i, rows[i].ToolName, arguments, rows[i].Sequence));
        }

        AiTranscriptBudget.Apply(bodies, toolRows);

        // ---- the replay ------------------------------------------------------------------------
        // A tool_result may only replay against a tool_use that actually made it into the
        // transcript — an orphan pair is rejected by the API and takes the whole turn down.
        // Ids accumulate as assistant rows are walked (they always precede their tool rows).
        //
        // Content is built as mutable block lists (not anonymous types) because the tail of the
        // transcript is edited after the walk: the newest persisted block gets the moving
        // cache_control breakpoint, and the turn-context block is appended after it.
        var replayedToolUseIds = new HashSet<string>(StringComparer.Ordinal);
        var transcript = new List<Dictionary<string, object?>>();
        // Context rows (a task's carried-over conversation) fold into the NEXT user message as a
        // leading text block — the API wants alternating roles, and context is background to the
        // user turn it precedes, not a turn of its own.
        var pendingContext = new List<Dictionary<string, object?>>();
        var index = 0;

        static Dictionary<string, object?> Text(string text) =>
            new() { ["type"] = "text", ["text"] = text };

        void AddMessage(string role, List<Dictionary<string, object?>> blocks) =>
            transcript.Add(new Dictionary<string, object?> { ["role"] = role, ["content"] = blocks });

        while (index < rows.Count)
        {
            var row = rows[index];

            switch ((AiChatRole)row.Role)
            {
                case AiChatRole.Context:
                    if (!string.IsNullOrWhiteSpace(bodies[index])) pendingContext.Add(Text(bodies[index]));
                    index++;
                    break;

                case AiChatRole.User:
                {
                    var blocks = new List<Dictionary<string, object?>>(pendingContext) { Text(bodies[index]) };
                    pendingContext.Clear();
                    AddMessage("user", blocks);
                    index++;
                    break;
                }

                case AiChatRole.Assistant:
                {
                    var blocks = new List<Dictionary<string, object?>>();
                    if (!string.IsNullOrWhiteSpace(bodies[index]))
                        blocks.Add(Text(bodies[index]));

                    foreach (var call in ReadToolCalls(row.ToolCallsJson))
                    {
                        replayedToolUseIds.Add(call.Id!);
                        blocks.Add(new Dictionary<string, object?>
                        {
                            ["type"] = "tool_use",
                            ["id"] = call.Id!,
                            ["name"] = call.Name!,
                            ["input"] = ParseInput(call.Input)
                        });
                    }

                    // An assistant row with neither text nor tool calls would be an empty content
                    // array, which the API rejects. Skip it.
                    if (blocks.Count > 0) AddMessage("assistant", blocks);
                    index++;
                    break;
                }

                case AiChatRole.Tool:
                {
                    var results = new List<Dictionary<string, object?>>();
                    while (index < rows.Count && (AiChatRole)rows[index].Role == AiChatRole.Tool)
                    {
                        var toolRow = rows[index];
                        // Prose fallback covers two cases: legacy rows written before tool_use
                        // blocks were persisted, and rows whose paired tool_use could not be
                        // replayed — a tool_result without its tool_use is rejected by the API.
                        var paired = !string.IsNullOrWhiteSpace(toolRow.ToolUseId)
                                     && replayedToolUseIds.Contains(toolRow.ToolUseId!);
                        results.Add(paired
                            ? new Dictionary<string, object?>
                            {
                                ["type"] = "tool_result",
                                ["tool_use_id"] = toolRow.ToolUseId!,
                                ["content"] = bodies[index]
                            }
                            : Text($"[earlier result from {toolRow.ToolName}]\n{bodies[index]}"));
                        index++;
                    }
                    AddMessage("user", results);
                    break;
                }

                default:
                    index++;
                    break;
            }
        }

        // ---- the tail --------------------------------------------------------------------------
        // The hop always ends on a user-role message (the new user turn, or the tool results). Two
        // edits, in this order:
        //   1. The moving cache breakpoint goes on the newest PERSISTED block, so next hop's replay
        //      is a byte-identical prefix of this one and reads back from cache.
        //   2. The turn context (where the user is, dialog contents, look-up budget) is appended
        //      AFTER the breakpoint — it changes every hop, and sitting outside the cached prefix
        //      is exactly what lets it change for free.
        if (transcript.Count > 0
            && transcript[^1]["content"] is List<Dictionary<string, object?>> { Count: > 0 } tail
            && Equals(transcript[^1]["role"], "user"))
        {
            tail[^1]["cache_control"] = new { type = "ephemeral" };
            tail.Add(Text(turnContext));
        }

        return transcript.Cast<object>().ToList();
    }

    private sealed record StoredToolCall(string? Id, string? Name, string? Input);

    /// <summary>ToolCallsJson is written lowercase ({id, name, input}) and System.Text.Json is
    /// case-SENSITIVE by default — without this option every stored call deserialised to nulls,
    /// which broke the tool_use replay on every continuation hop (JPMS-B55A7A).</summary>
    private static readonly JsonSerializerOptions StoredToolCallJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static IReadOnlyList<StoredToolCall> ReadToolCalls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<StoredToolCall>();
        try
        {
            var calls = JsonSerializer.Deserialize<List<StoredToolCall>>(json, StoredToolCallJson)
                        ?? new List<StoredToolCall>();
            // A call with no id or name cannot be replayed as a tool_use block (the API rejects
            // it) and cannot key a lookup. Drop it rather than letting a bad row kill the turn —
            // its tool RESULT still replays, as prose, via the legacy branch below.
            return calls
                .Where(call => !string.IsNullOrWhiteSpace(call.Id) && !string.IsNullOrWhiteSpace(call.Name))
                .ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<StoredToolCall>();
        }
    }

    private static JsonElement ParseInput(string? input)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(input) ? "{}" : input);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            using var empty = JsonDocument.Parse("{}");
            return empty.RootElement.Clone();
        }
    }

    // ---- plumbing --------------------------------------------------------------------------

    private AiConversationMessageEntity Add(
        AiConversationEntity conversation, AiChatRole role, string body, int sequence,
        List<AiConversationMessageEntity> collected)
    {
        var entity = new AiConversationMessageEntity
        {
            MessageId = Guid.NewGuid().ToString("N"),
            ConversationId = conversation.ConversationId,
            Role = (int)role,
            Body = body,
            Sequence = sequence,
            PostedAt = DateTimeOffset.UtcNow
        };
        context.AiConversationMessages.Add(entity);
        collected.Add(entity);
        return entity;
    }

    private async Task SaveAsync(AiConversationEntity conversation, CancellationToken ct)
    {
        conversation.LastMessageAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    private Task LogAsync(
        AiConversationEntity conversation, SignedInUser user, AiScope? scope,
        AgentOutcome outcome, string summary, IReadOnlyList<AiStep> steps,
        Stopwatch clock, int inputTokens, int outputTokens, CancellationToken ct) =>
        activityLog.WriteAsync(
            agentKey: conversation.CapabilityKey,
            trigger: AgentTrigger.Chat,
            actorEmail: user.Email,
            action: "chat.hop",
            outcome: outcome,
            summary: summary,
            cancellationToken: ct,
            conversationId: conversation.ConversationId,
            projectId: scope?.ProjectId,
            route: scope?.Route,
            toolsUsed: steps.Select(step => step.Tool),
            durationMs: (int)clock.ElapsedMilliseconds,
            inputTokens: inputTokens,
            outputTokens: outputTokens);

    private static AiTurnResult Result(
        AiConversationEntity conversation, AiTurnStatus status,
        IEnumerable<AiConversationMessageEntity> messages,
        IReadOnlyList<AiUiAction> uiActions, IReadOnlyList<AiStep> steps, int remaining,
        string? modelNote = null) =>
        new(conversation.ConversationId,
            status,
            messages
                .Where(row => row.Role != (int)AiChatRole.Tool && !string.IsNullOrWhiteSpace(row.Body))
                .Select(row => new AiChatMessage(
                    row.MessageId, (AiChatRole)row.Role, row.Body, row.ToolName, row.PostedAt))
                .ToList(),
            uiActions,
            steps,
            remaining,
            // Post-switch: a switch_agent call in this hop is already on the conversation, so the
            // panel can show the new hat as soon as the hop returns.
            conversation.CapabilityKey,
            modelNote);

    private static string Fail(string message) => JsonSerializer.Serialize(new { ok = false, error = message });

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..length];
}
