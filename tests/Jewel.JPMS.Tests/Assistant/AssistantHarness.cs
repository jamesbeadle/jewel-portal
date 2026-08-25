using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.Ai.Commands;
using Jewel.JPMS.Api.Features.Ai.Storage;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jewel.JPMS.Tests.Assistant;

/// <summary>
/// The assistant's regression harness (docs/ai/06-context-retrieval.md, Phase 4): runs REAL turns
/// through the real AiTurnRunner — the real tool catalogue, the real Ui-action validation, the
/// real transcript and turn-context building, the real database queries against an in-memory
/// JpmsContext — with only the Claude call replaced by a script of replies. Every guard the
/// live failures taught us is deterministic server code, so this is where each failure becomes
/// a check that runs on every build, without a token being billed.
///
/// <para>What it cannot test is the model's own choices (which tool it reaches for). That is the
/// live smoke after a deploy; this pins that when the model does what it did on the day, the
/// server now answers correctly.</para>
/// </summary>
public sealed class AssistantHarness : IDisposable
{
    public JpmsContext Db { get; }
    public ScriptedClaude Claude { get; } = new();
    public InMemoryAttachmentStore Attachments { get; } = new();
    public SignedInUser User { get; }

    private readonly ServiceProvider services;
    private readonly AiTurnRunner runner;

    public AssistantHarness(SignedInUser? user = null)
    {
        User = user ?? new SignedInUser("qs@jewelbb.co.uk", "Test QS", new[] { Role.QuantitySurveyor });

        var options = new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase("assistant-" + Guid.NewGuid().ToString("N"))
            .Options;
        Db = new JpmsContext(options);

        var collection = new ServiceCollection();
        collection.AddSingleton(Db);
        collection.AddSingleton<IAiAttachmentStore>(Attachments);
        collection.AddSingleton(new AnthropicOptions { ApiKey = "test" });
        collection.AddSingleton<ILogger<AgentActivityLog>>(NullLogger<AgentActivityLog>.Instance);
        collection.AddSingleton<AgentActivityLog>();
        collection.AddSingleton<IClaudeConversationClient>(Claude);
        services = collection.BuildServiceProvider();

        runner = new AiTurnRunner(Db, Claude, services.GetRequiredService<AgentActivityLog>(), services);
    }

    /// <summary>Starts a conversation on a route and sends one user message, pumping hops the way
    /// the panel does until the turn completes. Returns everything that happened.</summary>
    public async Task<TurnOutcome> SendAsync(string message, AiScope scope, string? conversationId = null)
    {
        var conversation = conversationId is null
            ? await StartConversationAsync(scope)
            : await Db.AiConversations.FirstAsync(row => row.ConversationId == conversationId);

        var sequence = await Db.AiConversationMessages
            .Where(row => row.ConversationId == conversation.ConversationId)
            .Select(row => (int?)row.Sequence).MaxAsync() ?? 0;
        Db.AiConversationMessages.Add(new AiConversationMessageEntity
        {
            MessageId = Guid.NewGuid().ToString("N"),
            ConversationId = conversation.ConversationId,
            Role = (int)AiChatRole.User,
            Body = message,
            Sequence = sequence + 1,
            PostedAt = DateTimeOffset.UtcNow
        });
        await Db.SaveChangesAsync();

        var uiActions = new List<AiUiAction>();
        var steps = new List<AiStep>();
        AiTurnResult result;
        var hops = 0;
        do
        {
            result = await runner.RunHopAsync(conversation, User, scope, null, CancellationToken.None);
            uiActions.AddRange(result.UiActions);
            steps.AddRange(result.Steps);
            hops++;
        }
        while (result.Status == AiTurnStatus.NeedsContinue && hops < 12);

        var rows = await Db.AiConversationMessages
            .Where(row => row.ConversationId == conversation.ConversationId)
            .OrderBy(row => row.Sequence)
            .ToListAsync();

        return new TurnOutcome(conversation.ConversationId, result.Status, uiActions, steps, rows,
            rows.LastOrDefault(row => row.Role == (int)AiChatRole.Assistant && row.ToolCallsJson == null)?.Body ?? "");
    }

    /// <summary>Attaches a file the way the panel's upload does — through the real handler.</summary>
    public async Task<AiAttachmentReceipt> AttachAsync(string fileName, byte[] content, AiScope scope, string? conversationId = null)
    {
        var handler = new AddAiAttachmentHandler(Db, new AiCaller { Current = User }, Attachments);
        return await handler.HandleAsync(
            new AddAiAttachment(conversationId, fileName, Convert.ToBase64String(content), scope, User.Email),
            CancellationToken.None);
    }

    private async Task<AiConversationEntity> StartConversationAsync(AiScope scope)
    {
        var now = DateTimeOffset.UtcNow;
        var conversation = new AiConversationEntity
        {
            ConversationId = Guid.NewGuid().ToString("N"),
            ProjectId = scope.ProjectId,
            Route = scope.Route,
            ScopeRecordType = scope.RecordType,
            ScopeRecordId = scope.RecordId,
            CapabilityKey = AgentCatalogue.ForRoute(scope.Route, User.Roles).Key,
            StartedByEmail = User.Email,
            Title = "Harness",
            StartedAt = now,
            LastMessageAt = now
        };
        Db.AiConversations.Add(conversation);
        await Db.SaveChangesAsync();
        return conversation;
    }

    public void Dispose()
    {
        services.Dispose();
        Db.Dispose();
    }
}

/// <summary>Everything one turn produced: the browser actions, the steps, the stored rows and the
/// final reply — plus the tool results by name so a scenario can assert on what the model was told.</summary>
public sealed record TurnOutcome(
    string ConversationId,
    AiTurnStatus Status,
    IReadOnlyList<AiUiAction> UiActions,
    IReadOnlyList<AiStep> Steps,
    IReadOnlyList<AiConversationMessageEntity> Rows,
    string Reply)
{
    /// <summary>The stored results of every call to <paramref name="toolName"/>, in order.</summary>
    public IReadOnlyList<string> ToolResults(string toolName) =>
        Rows.Where(row => row.Role == (int)AiChatRole.Tool
                          && string.Equals(row.ToolName, toolName, StringComparison.OrdinalIgnoreCase))
            .Select(row => row.Body)
            .ToList();

    public string LastToolResult(string toolName) =>
        ToolResults(toolName).LastOrDefault() ?? throw new InvalidOperationException($"No {toolName} result in the turn.");
}

/// <summary>
/// The model, scripted: each hop dequeues the next reply. A reply with tool calls makes the
/// runner run them and ask again; a reply with none ends the turn. Everything the runner sent —
/// system prompt, transcript, tools — is captured per hop so a scenario can assert on what the
/// model would have SEEN (the turn context's "files on hand" block above all).
/// </summary>
public sealed class ScriptedClaude : IClaudeConversationClient
{
    private readonly Queue<ClaudeReply> replies = new();
    public List<CapturedCall> Calls { get; } = new();

    public bool IsConfigured => true;

    public ScriptedClaude Then(params ClaudeToolCall[] toolCalls)
    {
        replies.Enqueue(new ClaudeReply(true, toolCalls.Length == 0 ? "Done." : "Working…", toolCalls, "end_turn", null));
        return this;
    }

    public ScriptedClaude ThenSay(string text)
    {
        replies.Enqueue(new ClaudeReply(true, text, Array.Empty<ClaudeToolCall>(), "end_turn", null));
        return this;
    }

    public Task<ClaudeReply> ContinueAsync(
        string systemPrompt, IReadOnlyList<object> messages, IReadOnlyList<ClaudeToolSpec> tools, string? modelTier, CancellationToken ct)
    {
        Calls.Add(new CapturedCall(systemPrompt, messages, tools));
        if (replies.Count == 0)
            return Task.FromResult(new ClaudeReply(true, "(no more scripted replies)", Array.Empty<ClaudeToolCall>(), "end_turn", null));
        return Task.FromResult(replies.Dequeue());
    }

    /// <summary>The text of the newest user-side block the model saw on a call — where the turn
    /// context rides — flattened so a scenario can search it.</summary>
    public static string LastUserText(CapturedCall call)
    {
        var last = call.Messages.LastOrDefault() as Dictionary<string, object?>;
        if (last?["content"] is not List<Dictionary<string, object?>> blocks) return "";
        return string.Join("\n", blocks
            .Where(block => block.TryGetValue("type", out var type) && Equals(type, "text"))
            .Select(block => block["text"]?.ToString() ?? ""));
    }

    public static ClaudeToolCall Call(string name, object arguments) =>
        new(Guid.NewGuid().ToString("N")[..12], name, System.Text.Json.JsonSerializer.Serialize(arguments));
}

public sealed record CapturedCall(string SystemPrompt, IReadOnlyList<object> Messages, IReadOnlyList<ClaudeToolSpec> Tools);

/// <summary>Blob storage in a dictionary — enough to round-trip an upload through read_source.</summary>
public sealed class InMemoryAttachmentStore : IAiAttachmentStore
{
    private readonly Dictionary<string, byte[]> blobs = new(StringComparer.Ordinal);

    public bool IsConfigured => true;

    public Task<string> UploadAsync(string conversationId, string attachmentId, string fileName, string contentType, byte[] content, CancellationToken cancellationToken)
    {
        var blobRef = $"conversations/{conversationId}/{attachmentId}/{fileName}";
        blobs[blobRef] = content;
        return Task.FromResult(blobRef);
    }

    public Task<byte[]?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
        Task.FromResult(blobs.TryGetValue(blobRef, out var bytes) ? bytes : null);

    public Task DeleteAsync(string blobRef, CancellationToken cancellationToken)
    {
        blobs.Remove(blobRef);
        return Task.CompletedTask;
    }
}
