using System.Diagnostics;
using System.Text.Json;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Ai.Commands;

/// <summary>
/// One user turn, start to finish, inside a single request.
///
/// <para>The server is authoritative: the transcript is rebuilt from the database every time, so a
/// tampered client cannot inject an assistant turn, forge a tool result, or reach the system prompt.
/// The only thing it contributes is the user's own message.</para>
///
/// <para>The loop runs server-side because every tool here is a read. It is bounded twice — by
/// <see cref="MaxSteps"/> and by <see cref="BudgetSeconds"/> — because the Static Web Apps managed-
/// functions gateway gives up at roughly 45 seconds and a half-finished reply beats a 502.</para>
/// </summary>
public sealed class SendAiMessageHandler : ICommandHandler<SendAiMessage, AiTurnResult>
{
    /// <summary>Claude round trips per user message. Enough for look-up → look-up → answer.</summary>
    private const int MaxSteps = 4;

    /// <summary>Wall clock for the whole turn, comfortably inside the gateway timeout.</summary>
    private const int BudgetSeconds = 32;

    private readonly JpmsContext context;
    private readonly IClaudeConversationClient claude;
    private readonly AiCaller caller;
    private readonly AgentActivityLog activityLog;

    public SendAiMessageHandler(
        JpmsContext context, IClaudeConversationClient claude, AiCaller caller, AgentActivityLog activityLog)
    {
        this.context = context;
        this.claude = claude;
        this.caller = caller;
        this.activityLog = activityLog;
    }

    public async Task<AiTurnResult> HandleAsync(SendAiMessage command, CancellationToken cancellationToken)
    {
        var user = caller.Current
            ?? throw new InvalidOperationException("The assistant needs a signed-in user.");

        // Every exit path below logs exactly one activity row — a run that failed is precisely the
        // one worth being able to see.
        var runClock = Stopwatch.StartNew();
        var inputTokens = 0;
        var outputTokens = 0;

        var conversation = await LoadOrStartConversationAsync(command, cancellationToken);
        var sequence = await context.AiConversationMessages
            .Where(row => row.ConversationId == conversation.ConversationId)
            .Select(row => (int?)row.Sequence)
            .MaxAsync(cancellationToken) ?? 0;

        var newMessages = new List<AiConversationMessageEntity>();
        var userMessage = NewMessage(conversation.ConversationId, AiChatRole.User, command.Message, ++sequence);
        newMessages.Add(userMessage);
        context.AiConversationMessages.Add(userMessage);
        await context.SaveChangesAsync(cancellationToken);

        if (!claude.IsConfigured)
        {
            var unavailable = NewMessage(
                conversation.ConversationId, AiChatRole.Assistant,
                "The assistant is not connected yet — no Anthropic API key is configured on this environment. "
                + "Ask an administrator to set Anthropic__ApiKey.",
                ++sequence);
            context.AiConversationMessages.Add(unavailable);
            await SaveAndTouchAsync(conversation, cancellationToken);
            await LogAsync(command, conversation, AgentOutcome.NotConfigured,
                "No Anthropic API key is configured on this environment.",
                Array.Empty<string>(), runClock, 0, 0, cancellationToken);
            return Result(conversation, AiTurnStatus.Unavailable, new[] { userMessage, unavailable },
                Array.Empty<AiUiAction>(), Array.Empty<string>());
        }

        // ---- Context for the prompt: the project in view, if there is one. ----
        var project = string.IsNullOrWhiteSpace(command.Scope?.ProjectId)
            ? null
            : await context.Projects.AsNoTracking()
                .FirstOrDefaultAsync(row => row.ProjectId == command.Scope!.ProjectId, cancellationToken);

        var systemPrompt = AiSystemPrompt.Build(user, command.Scope, project?.Reference, project?.Name);
        var tools = AiToolCatalogue.For(user)
            .Select(tool => new ClaudeToolSpec(tool.Name, tool.Description, tool.InputSchema))
            .ToList();

        var transcript = await BuildTranscriptAsync(conversation.ConversationId, cancellationToken);
        var toolContext = new AiToolContext(context, user, command.Scope);

        var uiActions = new List<AiUiAction>();
        var toolsUsed = new List<string>();
        var status = AiTurnStatus.Complete;
        var clock = Stopwatch.StartNew();

        for (var step = 0; step < MaxSteps; step++)
        {
            if (clock.Elapsed.TotalSeconds > BudgetSeconds)
            {
                status = AiTurnStatus.Truncated;
                break;
            }

            var reply = await claude.ContinueAsync(systemPrompt, transcript, tools, cancellationToken);
            inputTokens += reply.InputTokens;
            outputTokens += reply.OutputTokens;

            if (!reply.Ok)
            {
                var failed = NewMessage(
                    conversation.ConversationId, AiChatRole.Assistant,
                    "I could not reach the Claude API just then. Try again in a moment — nothing has been changed.",
                    ++sequence);
                newMessages.Add(failed);
                context.AiConversationMessages.Add(failed);
                await SaveAndTouchAsync(conversation, cancellationToken);
                await LogAsync(command, conversation, AgentOutcome.Failed,
                    $"The Claude API call failed ({reply.Error}).",
                    toolsUsed, runClock, inputTokens, outputTokens, cancellationToken);
                return Result(conversation, AiTurnStatus.Unavailable, newMessages, uiActions, toolsUsed);
            }

            // Persist whatever the model said, even alongside a tool call — it usually explains itself.
            if (!string.IsNullOrWhiteSpace(reply.Text))
            {
                var assistant = NewMessage(
                    conversation.ConversationId, AiChatRole.Assistant, reply.Text!, ++sequence);
                newMessages.Add(assistant);
                context.AiConversationMessages.Add(assistant);
            }

            if (reply.ToolCalls.Count == 0)
            {
                await SaveAndTouchAsync(conversation, cancellationToken);
                break;
            }

            // The assistant turn has to go back verbatim, tool_use blocks included, or Anthropic
            // rejects the tool_result that follows it.
            transcript.Add(new
            {
                role = "assistant",
                content = BuildAssistantContent(reply)
            });

            var results = new List<object>();
            foreach (var call in reply.ToolCalls)
            {
                toolsUsed.Add(call.Name);
                var tool = AiToolCatalogue.Find(call.Name);

                string output;
                if (tool is null)
                {
                    output = JsonSerializer.Serialize(new { ok = false, error = $"No tool named {call.Name}." });
                }
                else if (!tool.VisibleTo.IncludesAny(user.Roles))
                {
                    // Belt and braces: the catalogue was already filtered, so reaching here means the
                    // model invented a name it was never given.
                    output = JsonSerializer.Serialize(new { ok = false, error = "You are not permitted to use that tool." });
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
                        output = JsonSerializer.Serialize(new { ok = false, error = $"The tool failed: {ex.Message}" });
                    }
                }

                var toolRow = NewMessage(conversation.ConversationId, AiChatRole.Tool, output, ++sequence);
                toolRow.ToolName = call.Name;
                toolRow.ToolUseId = call.Id;
                context.AiConversationMessages.Add(toolRow);

                results.Add(new { type = "tool_result", tool_use_id = call.Id, content = output });
            }

            transcript.Add(new { role = "user", content = results.ToArray() });
            await context.SaveChangesAsync(cancellationToken);

            if (step == MaxSteps - 1) status = AiTurnStatus.Truncated;
        }

        await SaveAndTouchAsync(conversation, cancellationToken);

        // A turn that ended on a tool call with nothing said needs *something* in the panel.
        if (!newMessages.Any(row => row.Role == (int)AiChatRole.Assistant))
        {
            var filler = NewMessage(
                conversation.ConversationId, AiChatRole.Assistant,
                "I looked that up but ran out of room to answer. Ask me again and I'll pick it up.",
                ++sequence);
            newMessages.Add(filler);
            context.AiConversationMessages.Add(filler);
            await SaveAndTouchAsync(conversation, cancellationToken);
        }

        await LogAsync(command, conversation,
            status == AiTurnStatus.Truncated ? AgentOutcome.Truncated : AgentOutcome.Ok,
            Truncate(command.Message, 400),
            toolsUsed, runClock, inputTokens, outputTokens, cancellationToken);

        return Result(conversation, status, newMessages, uiActions, toolsUsed);
    }

    /// <summary>
    /// One row per turn. Trigger is always Chat here — a person typed something — so IsAutonomous is
    /// false. The scheduled agents will write to the same table with Trigger.Schedule, which is what
    /// makes "show me only what ran unattended" a single filter.
    /// </summary>
    private Task LogAsync(
        SendAiMessage command,
        AiConversationEntity conversation,
        AgentOutcome outcome,
        string summary,
        IReadOnlyList<string> toolsUsed,
        Stopwatch clock,
        int inputTokens,
        int outputTokens,
        CancellationToken ct) =>
        activityLog.WriteAsync(
            agentKey: conversation.CapabilityKey,
            trigger: AgentTrigger.Chat,
            actorEmail: command.SentByEmail,
            action: "chat.turn",
            outcome: outcome,
            summary: summary,
            cancellationToken: ct,
            conversationId: conversation.ConversationId,
            projectId: command.Scope?.ProjectId,
            route: command.Scope?.Route,
            toolsUsed: toolsUsed,
            durationMs: (int)clock.ElapsedMilliseconds,
            inputTokens: inputTokens,
            outputTokens: outputTokens);

    /// <summary>Rebuilds the Anthropic messages array from the database. Tool rows are paired back to
    /// their calls by <c>ToolUseId</c>; a row without one is a plain turn.</summary>
    private async Task<List<object>> BuildTranscriptAsync(string conversationId, CancellationToken ct)
    {
        var rows = await context.AiConversationMessages
            .AsNoTracking()
            .Where(row => row.ConversationId == conversationId)
            .OrderBy(row => row.Sequence)
            .ToListAsync(ct);

        var transcript = new List<object>();
        foreach (var row in rows)
        {
            switch ((AiChatRole)row.Role)
            {
                case AiChatRole.User:
                    transcript.Add(new { role = "user", content = row.Body });
                    break;
                case AiChatRole.Assistant:
                    if (!string.IsNullOrWhiteSpace(row.Body))
                        transcript.Add(new { role = "assistant", content = row.Body });
                    break;
                case AiChatRole.Tool:
                    // Replayed as prose rather than a tool_result block: the assistant turn that
                    // requested it is not stored with its tool_use blocks, and an orphan tool_result
                    // is rejected by the API. Within a turn the live blocks are used instead.
                    transcript.Add(new
                    {
                        role = "user",
                        content = $"[earlier result from {row.ToolName}]\n{row.Body}"
                    });
                    break;
            }
        }

        return transcript;
    }

    private static object[] BuildAssistantContent(ClaudeReply reply)
    {
        var blocks = new List<object>();
        if (!string.IsNullOrWhiteSpace(reply.Text))
            blocks.Add(new { type = "text", text = reply.Text });

        foreach (var call in reply.ToolCalls)
        {
            using var arguments = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson);
            blocks.Add(new
            {
                type = "tool_use",
                id = call.Id,
                name = call.Name,
                input = JsonSerializer.Deserialize<JsonElement>(arguments.RootElement.GetRawText())
            });
        }

        return blocks.ToArray();
    }

    private async Task<AiConversationEntity> LoadOrStartConversationAsync(
        SendAiMessage command, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(command.ConversationId))
        {
            var existing = await context.AiConversations
                .FirstOrDefaultAsync(row => row.ConversationId == command.ConversationId, ct);

            // Scoped to the person who started it — a conversation id is not a capability.
            if (existing is not null
                && string.Equals(existing.StartedByEmail, command.SentByEmail, StringComparison.OrdinalIgnoreCase))
            {
                return existing;
            }
        }

        var now = DateTimeOffset.UtcNow;
        var conversation = new AiConversationEntity
        {
            ConversationId = Guid.NewGuid().ToString("N"),
            ProjectId = command.Scope?.ProjectId,
            Route = command.Scope?.Route,
            CapabilityKey = "orchestrator",
            StartedByEmail = command.SentByEmail,
            Title = Truncate(command.Message, 120),
            StartedAt = now,
            LastMessageAt = now
        };
        context.AiConversations.Add(conversation);
        return conversation;
    }

    private async Task SaveAndTouchAsync(AiConversationEntity conversation, CancellationToken ct)
    {
        conversation.LastMessageAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    private static AiConversationMessageEntity NewMessage(
        string conversationId, AiChatRole role, string body, int sequence) => new()
    {
        MessageId = Guid.NewGuid().ToString("N"),
        ConversationId = conversationId,
        Role = (int)role,
        Body = body,
        Sequence = sequence,
        PostedAt = DateTimeOffset.UtcNow
    };

    private static AiTurnResult Result(
        AiConversationEntity conversation,
        AiTurnStatus status,
        IEnumerable<AiConversationMessageEntity> messages,
        IReadOnlyList<AiUiAction> uiActions,
        IReadOnlyList<string> toolsUsed) =>
        new(conversation.ConversationId,
            status,
            messages
                .Where(row => row.Role != (int)AiChatRole.Tool)
                .Select(row => new AiChatMessage(
                    row.MessageId, (AiChatRole)row.Role, row.Body, row.ToolName, row.PostedAt))
                .ToList(),
            uiActions,
            toolsUsed);

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..length];
}
