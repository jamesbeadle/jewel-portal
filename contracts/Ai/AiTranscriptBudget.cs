namespace Jewel.JPMS.Contracts.Ai;

/// <summary>
/// One tool-result row of a transcript, as the budget sees it: where it sits, what produced it,
/// and with what arguments. <paramref name="ArgumentsJson"/> is the tool_use input as stored —
/// used only as an identity key, never parsed.
/// </summary>
public sealed record TranscriptToolRow(int Index, string? ToolName, string? ArgumentsJson, int Sequence);

/// <summary>
/// The two bounds that keep a long conversation affordable. The whole transcript is re-sent to the
/// model on every turn, so a 30k-character email thread fetched on turn one would otherwise be
/// paid for again on every turn of a ten-turn drafting conversation.
///
/// <para>Both bounds stub TOOL rows only. A user or assistant turn is never touched — that is the
/// conversation itself, and losing a line of it loses the thread. Pure and in contracts so the
/// test project can pin the behaviour (it references contracts alone).</para>
/// </summary>
public static class AiTranscriptBudget
{
    /// <summary>
    /// Tools whose result is large and whose earlier copies are pure cost — a second call WITH THE
    /// SAME ARGUMENTS supersedes the first. Keyed on name + arguments, not name alone: two
    /// different requests' working papers, or two different skills, are both worth keeping.
    /// </summary>
    private static readonly HashSet<string> ReplayLatestOnly =
        new(StringComparer.OrdinalIgnoreCase) { "get_request_context", "load_skill", "load_skill_reference" };

    /// <summary>
    /// The ceiling on a replayed transcript, in characters. Well inside the model's context
    /// window; the real point is the bill and the per-turn latency, both of which scale with what
    /// is sent again. Sized so one full get_request_context result plus a long drafting
    /// conversation fits without stubbing.
    /// </summary>
    public const int MaxTranscriptChars = 110_000;

    /// <summary>
    /// Applies both bounds to <paramref name="bodies"/> in place. <paramref name="toolRows"/>
    /// identifies which indexes are tool results; every other index is left alone.
    /// </summary>
    public static void Apply(string[] bodies, IReadOnlyList<TranscriptToolRow> toolRows)
    {
        StubSuperseded(bodies, toolRows);
        StubToBudget(bodies, toolRows);
    }

    private static void StubSuperseded(string[] bodies, IReadOnlyList<TranscriptToolRow> toolRows)
    {
        // Key → the newest sequence that produced this exact call.
        var newest = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in toolRows)
        {
            if (Key(row) is not { } key) continue;
            if (!newest.TryGetValue(key, out var sequence) || row.Sequence > sequence)
                newest[key] = row.Sequence;
        }

        if (newest.Count == 0) return;

        foreach (var row in toolRows)
        {
            if (Key(row) is not { } key) continue;
            if (newest[key] == row.Sequence) continue;

            bodies[row.Index] =
                $"(superseded by a later call to {row.ToolName} in this conversation — read that one instead)";
        }
    }

    private static void StubToBudget(string[] bodies, IReadOnlyList<TranscriptToolRow> toolRows)
    {
        var total = 0;
        foreach (var body in bodies) total += body.Length;
        if (total <= MaxTranscriptChars) return;

        // Oldest first: the recent legs of a conversation are the ones being reasoned from, and
        // the model is told plainly what went rather than left to wonder why it has forgotten.
        foreach (var row in toolRows.OrderBy(r => r.Sequence))
        {
            if (total <= MaxTranscriptChars) return;

            var stub = $"(dropped to keep this conversation affordable — call {row.ToolName} again if you need it)";
            if (bodies[row.Index].Length <= stub.Length) continue;

            total -= bodies[row.Index].Length - stub.Length;
            bodies[row.Index] = stub;
        }
    }

    private static string? Key(TranscriptToolRow row) =>
        row.ToolName is { } name && ReplayLatestOnly.Contains(name)
            ? $"{name.ToLowerInvariant()}::{row.ArgumentsJson ?? ""}"
            : null;
}
