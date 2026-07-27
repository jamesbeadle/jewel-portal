using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// Rebuilds the Anthropic messages array from the persisted conversation. Pure and static so it can
/// be tested — the shape of the transcript is what the whole turn is reasoned from, and it is not
/// something to find out about in production.
///
/// <para>The costly detail: the entire transcript is re-sent on EVERY turn. A tool result that ran
/// to tens of thousands of characters — a full RFI email thread — would otherwise be paid for again
/// on every subsequent message of a ten-turn drafting conversation, and would crowd out the reply.
/// Two bounds stop that, and both stub tool rows only. A user or assistant turn is never touched:
/// that is the conversation itself, and losing a line of it loses the thread.</para>
/// </summary>
public static class AiTranscript
{
    /// <summary>
    /// Tools whose result is large and whose earlier copies are pure cost — a second read of the
    /// same thing supersedes the first. The older rows collapse to a stub that still records that
    /// the call happened, so the model knows it can ask again rather than assuming it never asked.
    /// </summary>
    private static readonly HashSet<string> ReplayLatestOnly =
        new(StringComparer.OrdinalIgnoreCase) { "get_request_context" };

    /// <summary>
    /// The ceiling on a replayed transcript. Well inside the model's context window; the real point
    /// is the bill and the per-turn latency, both of which scale with what is sent again.
    ///
    /// <para>Sized so one full get_request_context result plus a long drafting conversation fits
    /// without stubbing. That result can run past AiToolCatalogue.MaxConversationChars (50k),
    /// because the assembler holds every message to a floor rather than dropping any — so the
    /// headroom here is deliberate. Only ONE copy of that tool's result is ever replayed (see
    /// ReplayLatestOnly), so it does not compound across turns.</para>
    /// </summary>
    private const int MaxTranscriptChars = 110_000;

    public static List<object> Build(IReadOnlyList<AiConversationMessageEntity> rows)
    {
        var bodies = new string[rows.Count];
        for (var index = 0; index < rows.Count; index++) bodies[index] = rows[index].Body ?? "";

        StubSuperseded(rows, bodies);
        StubToBudget(rows, bodies);

        var transcript = new List<object>();
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            switch ((AiChatRole)row.Role)
            {
                case AiChatRole.User:
                    transcript.Add(new { role = "user", content = bodies[index] });
                    break;

                case AiChatRole.Assistant:
                    if (!string.IsNullOrWhiteSpace(bodies[index]))
                        transcript.Add(new { role = "assistant", content = bodies[index] });
                    break;

                case AiChatRole.Tool:
                    // Replayed as prose rather than a tool_result block: the assistant turn that
                    // requested it is not stored with its tool_use blocks, and an orphan tool_result
                    // is rejected by the API. Within a turn the live blocks are used instead.
                    transcript.Add(new
                    {
                        role = "user",
                        content = $"[earlier result from {row.ToolName}]\n{bodies[index]}"
                    });
                    break;
            }
        }

        return transcript;
    }

    private static void StubSuperseded(IReadOnlyList<AiConversationMessageEntity> rows, string[] bodies)
    {
        var newest = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if ((AiChatRole)row.Role != AiChatRole.Tool) continue;
            if (row.ToolName is not { } name || !ReplayLatestOnly.Contains(name)) continue;
            if (!newest.TryGetValue(name, out var sequence) || row.Sequence > sequence)
                newest[name] = row.Sequence;
        }

        if (newest.Count == 0) return;

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            if ((AiChatRole)row.Role != AiChatRole.Tool) continue;
            if (row.ToolName is not { } name) continue;
            if (!newest.TryGetValue(name, out var sequence) || row.Sequence == sequence) continue;

            bodies[index] =
                $"(superseded by a later call to {name} in this conversation — read that one instead)";
        }
    }

    private static void StubToBudget(IReadOnlyList<AiConversationMessageEntity> rows, string[] bodies)
    {
        var total = 0;
        foreach (var body in bodies) total += body.Length;
        if (total <= MaxTranscriptChars) return;

        // Oldest first: the recent legs of a conversation are the ones being reasoned from, and the
        // model is told plainly what went rather than left to wonder why it has forgotten something.
        for (var index = 0; index < rows.Count && total > MaxTranscriptChars; index++)
        {
            if ((AiChatRole)rows[index].Role != AiChatRole.Tool) continue;

            var stub = $"(dropped to keep this conversation affordable — call {rows[index].ToolName} "
                + "again if you need it)";
            if (bodies[index].Length <= stub.Length) continue;

            total -= bodies[index].Length - stub.Length;
            bodies[index] = stub;
        }
    }
}
