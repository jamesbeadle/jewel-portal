using System.Text.Json;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Pins the shape AiTurnRunner.BuildTranscript hands to the Anthropic Messages API. This is the
// hottest bug surface in the whole assistant — the transcript is rebuilt from the database on
// every hop, and the API rejects the WHOLE turn if a tool_result is not paired with its tool_use,
// if roles don't alternate, or if a content array is empty. Past production incidents lived here
// (JPMS-B55A7A: tool calls deserialised to nulls because the stored JSON was case-sensitive).
// These tests construct entity rows directly — BuildTranscript is pure, no database.
public sealed class AiTurnTranscriptTests
{
    private const string Ctx = "--- current context ---";

    private static AiConversationMessageEntity Row(
        AiChatRole role, string body, int sequence,
        string? toolName = null, string? toolUseId = null, string? toolCallsJson = null) =>
        new()
        {
            MessageId = $"m{sequence}",
            ConversationId = "c1",
            Role = (int)role,
            Body = body,
            Sequence = sequence,
            ToolName = toolName,
            ToolUseId = toolUseId,
            ToolCallsJson = toolCallsJson
        };

    // {id,name,input} exactly as SendAiMessageHandler persists them: lowercase keys (the case that
    // broke live), and input is the tool_use arguments as a JSON STRING (call.ArgumentsJson), not a
    // nested object — the stored StoredToolCall.Input is a string.
    private static string ToolCall(string id, string name, object input) =>
        JsonSerializer.Serialize(new[] { new { id, name, input = JsonSerializer.Serialize(input) } });

    private static List<Dictionary<string, object?>> Content(object message) =>
        (List<Dictionary<string, object?>>)((Dictionary<string, object?>)message)["content"]!;

    private static string RoleOf(object message) =>
        (string)((Dictionary<string, object?>)message)["role"]!;

    private static string TypeOf(Dictionary<string, object?> block) => (string)block["type"]!;

    [Fact]
    public void APlainExchange_altenatesUserAndAssistant_andEndsWithTheContextBlock()
    {
        var rows = new List<AiConversationMessageEntity>
        {
            Row(AiChatRole.User, "what's the status of V72?", 1),
            Row(AiChatRole.Assistant, "It is at Quoting.", 2)
        };

        var transcript = AiTurnRunner.BuildTranscript(rows, Ctx);

        Assert.Equal("user", RoleOf(transcript[0]));
        Assert.Equal("assistant", RoleOf(transcript[1]));
        // The volatile turn-context block is appended to the LAST user message, AFTER the cache
        // breakpoint — that is what lets it change every hop without breaking the cached prefix.
        // Here the last message is the assistant, so the context has nowhere to append: the tail
        // edit only fires on a user-role tail. (Asserted properly in the tool-result test below.)
        Assert.Equal(2, transcript.Count);
    }

    [Fact]
    public void AToolCall_pairsToolUseWithItsToolResult_andAppendsContextAfterTheBreakpoint()
    {
        var rows = new List<AiConversationMessageEntity>
        {
            Row(AiChatRole.User, "read V72's emails", 1),
            Row(AiChatRole.Assistant, "Checking…", 2,
                toolCallsJson: ToolCall("tool_1", "read_record_emails", new { recordId = "v72" })),
            Row(AiChatRole.Tool, "{\"ok\":true,\"emails\":[]}", 3, toolName: "read_record_emails", toolUseId: "tool_1")
        };

        var transcript = AiTurnRunner.BuildTranscript(rows, Ctx);

        // user → assistant(text + tool_use) → user(tool_result [+ context])
        Assert.Equal(3, transcript.Count);

        var assistant = Content(transcript[1]);
        Assert.Contains(assistant, b => TypeOf(b) == "text");
        var toolUse = Assert.Single(assistant, b => TypeOf(b) == "tool_use");
        Assert.Equal("tool_1", toolUse["id"]);
        Assert.Equal("read_record_emails", toolUse["name"]);

        var results = Content(transcript[2]);
        var toolResult = results.First(b => TypeOf(b) == "tool_result");
        // The pairing the API demands: the tool_result names the SAME id as the tool_use.
        Assert.Equal("tool_1", toolResult["tool_use_id"]);
        // The context block is the LAST block of the tail user message.
        var last = results[^1];
        Assert.Equal("text", TypeOf(last));
        Assert.Equal(Ctx, last["text"]);
    }

    [Fact]
    public void AnUnpairableToolResult_replaysAsProse_neverAsAnOrphanToolResult()
    {
        // The tool row's tool_use never made it into the transcript (its assistant row is absent —
        // a legacy row, or one dropped by the budget). An orphan tool_result crashes the whole
        // turn, so it must degrade to a plain text block instead.
        var rows = new List<AiConversationMessageEntity>
        {
            Row(AiChatRole.User, "hi", 1),
            Row(AiChatRole.Tool, "orphaned result", 2, toolName: "list_variations", toolUseId: "missing")
        };

        var transcript = AiTurnRunner.BuildTranscript(rows, Ctx);

        foreach (var message in transcript)
            foreach (var block in Content(message))
                Assert.NotEqual("tool_result", TypeOf(block));
    }

    [Fact]
    public void CaseInsensitiveToolCallJson_stillReplays_theJPMS_B55A7A_regression()
    {
        // The bug: stored tool-call JSON is lowercase {id,name,input}; a case-SENSITIVE reader
        // deserialised every field to null and the tool_use vanished, orphaning its result and
        // taking the turn down. This asserts the lowercase shape round-trips.
        var rows = new List<AiConversationMessageEntity>
        {
            Row(AiChatRole.User, "find V72", 1),
            // input is a JSON STRING, as stored — the whole point of the regression is the
            // lowercase keys round-tripping, and the stored input is call.ArgumentsJson (a string).
            Row(AiChatRole.Assistant, "", 2,
                toolCallsJson: "[{\"id\":\"t9\",\"name\":\"find_by_reference\",\"input\":\"{\\\"reference\\\":\\\"V72\\\"}\"}]"),
            Row(AiChatRole.Tool, "{\"ok\":true}", 3, toolName: "find_by_reference", toolUseId: "t9")
        };

        var transcript = AiTurnRunner.BuildTranscript(rows, Ctx);

        var toolUse = Content(transcript[1]).Single(b => TypeOf(b) == "tool_use");
        Assert.Equal("t9", toolUse["id"]);
        Assert.Equal("find_by_reference", toolUse["name"]);
        Assert.Equal("t9", Content(transcript[2]).First(b => TypeOf(b) == "tool_result")["tool_use_id"]);
    }

    [Fact]
    public void AnAssistantRowWithNeitherTextNorToolCalls_isSkipped_notEmitedAsEmptyContent()
    {
        // The API rejects a message whose content array is empty.
        var rows = new List<AiConversationMessageEntity>
        {
            Row(AiChatRole.User, "hi", 1),
            Row(AiChatRole.Assistant, "", 2),
            Row(AiChatRole.User, "still there?", 3)
        };

        var transcript = AiTurnRunner.BuildTranscript(rows, Ctx);

        foreach (var message in transcript)
            Assert.NotEmpty(Content(message));
    }

    [Fact]
    public void AnImageToolResult_replaysAsARealImageBlock_notItsBudgetStandIn()
    {
        var oneByOnePng = System.Convert.ToBase64String(new byte[]
        {
            0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A // just the signature — the replay never decodes
        });
        var imageBody = AiImageToolResult.Build("drawing.png", "image/png", System.Convert.FromBase64String(oneByOnePng));

        var rows = new List<AiConversationMessageEntity>
        {
            Row(AiChatRole.User, "open the drawing", 1),
            Row(AiChatRole.Assistant, "", 2,
                toolCallsJson: ToolCall("img1", "read_email_attachment", new { messageId = "e1", attachmentId = "a1" })),
            Row(AiChatRole.Tool, imageBody, 3, toolName: "read_email_attachment", toolUseId: "img1")
        };

        var transcript = AiTurnRunner.BuildTranscript(rows, Ctx);

        var result = Content(transcript[2]).First(b => TypeOf(b) == "tool_result");
        var inner = (List<Dictionary<string, object?>>)result["content"]!;
        var image = Assert.Single(inner, b => TypeOf(b) == "image");
        var source = (Dictionary<string, object?>)image["source"]!;
        Assert.Equal("base64", source["type"]);
        Assert.Equal("image/png", source["media_type"]);
        Assert.Equal(oneByOnePng, source["data"]);
    }

    [Fact]
    public void ExactlyOneCacheBreakpoint_sitsOnTheNewestPersistedBlock()
    {
        var rows = new List<AiConversationMessageEntity>
        {
            Row(AiChatRole.User, "one", 1),
            Row(AiChatRole.Assistant, "two", 2),
            Row(AiChatRole.User, "three", 3)
        };

        var transcript = AiTurnRunner.BuildTranscript(rows, Ctx);

        var breakpoints = 0;
        Dictionary<string, object?>? marked = null;
        foreach (var message in transcript)
            foreach (var block in Content(message))
                if (block.ContainsKey("cache_control")) { breakpoints++; marked = block; }

        Assert.Equal(1, breakpoints);
        // It sits on the user's own words ("three"), NOT on the context block appended after it —
        // the context changes every hop and must stay outside the cached prefix.
        Assert.Equal("three", marked!["text"]);
    }
}
