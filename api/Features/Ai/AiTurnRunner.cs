using System.Diagnostics;
using System.Text.Json;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Gates;
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
    /// <summary>Claude round trips per user message, across all hops. Enough for look-up →
    /// look-up → answer, and bounded so a confused model cannot spend indefinitely.</summary>
    public const int MaxHops = 6;

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

        var systemPrompt = AiSystemPrompt.Build(user, scope, project?.Reference, project?.Name);
        var tools = AiToolCatalogue.For(user)
            .Select(tool => new ClaudeToolSpec(tool.Name, tool.Description, tool.InputSchema))
            .ToList();

        var reply = await claude.ContinueAsync(systemPrompt, BuildTranscript(rows), tools, cancellationToken);

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

        var toolContext = new AiToolContext(context, user, scope, services);

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
            else if (tool.Kind == AiToolKind.Ui)
            {
                uiActions.Add(new AiUiAction(call.Name, call.ArgumentsJson));
                output = JsonSerializer.Serialize(new { ok = true, handed_to_browser = true });
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

        return Result(conversation, status, newMessages, uiActions, steps, remaining);
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
    private static List<object> BuildTranscript(List<AiConversationMessageEntity> rows)
    {
        var transcript = new List<object>();
        var index = 0;

        while (index < rows.Count)
        {
            var row = rows[index];

            switch ((AiChatRole)row.Role)
            {
                case AiChatRole.User:
                    transcript.Add(new { role = "user", content = row.Body });
                    index++;
                    break;

                case AiChatRole.Assistant:
                {
                    var blocks = new List<object>();
                    if (!string.IsNullOrWhiteSpace(row.Body))
                        blocks.Add(new { type = "text", text = row.Body });

                    foreach (var call in ReadToolCalls(row.ToolCallsJson))
                    {
                        blocks.Add(new
                        {
                            type = "tool_use",
                            id = call.Id,
                            name = call.Name,
                            input = ParseInput(call.Input)
                        });
                    }

                    // An assistant row with neither text nor tool calls would be an empty content
                    // array, which the API rejects. Skip it.
                    if (blocks.Count > 0) transcript.Add(new { role = "assistant", content = blocks.ToArray() });
                    index++;
                    break;
                }

                case AiChatRole.Tool:
                {
                    var results = new List<object>();
                    while (index < rows.Count && (AiChatRole)rows[index].Role == AiChatRole.Tool)
                    {
                        var toolRow = rows[index];
                        results.Add(string.IsNullOrWhiteSpace(toolRow.ToolUseId)
                            // Legacy rows written before tool_use blocks were persisted — replay as
                            // prose so an old conversation still loads rather than 400ing.
                            ? new { type = "text", text = $"[earlier result from {toolRow.ToolName}]\n{toolRow.Body}" }
                            : new { type = "tool_result", tool_use_id = toolRow.ToolUseId!, content = toolRow.Body });
                        index++;
                    }
                    transcript.Add(new { role = "user", content = results.ToArray() });
                    break;
                }

                default:
                    index++;
                    break;
            }
        }

        return transcript;
    }

    private sealed record StoredToolCall(string Id, string Name, string Input);

    private static IReadOnlyList<StoredToolCall> ReadToolCalls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<StoredToolCall>();
        try
        {
            return JsonSerializer.Deserialize<List<StoredToolCall>>(json) ?? new List<StoredToolCall>();
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
        IReadOnlyList<AiUiAction> uiActions, IReadOnlyList<AiStep> steps, int remaining) =>
        new(conversation.ConversationId,
            status,
            messages
                .Where(row => row.Role != (int)AiChatRole.Tool && !string.IsNullOrWhiteSpace(row.Body))
                .Select(row => new AiChatMessage(
                    row.MessageId, (AiChatRole)row.Role, row.Body, row.ToolName, row.PostedAt))
                .ToList(),
            uiActions,
            steps,
            remaining);

    private static string Fail(string message) => JsonSerializer.Serialize(new { ok = false, error = message });

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..length];
}
