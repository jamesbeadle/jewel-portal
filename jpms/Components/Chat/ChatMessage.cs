namespace Jewel.JPMS.Components.Chat;

public enum ChatAuthor { User, Assistant }

/// <summary>One turn in the transcript. <paramref name="SentAtLabel"/> is a pre-formatted display
/// string rather than a timestamp — the panel is a UI shell, and nothing here is persisted yet.</summary>
public sealed record ChatMessage(ChatAuthor Author, string Body, string SentAtLabel);

/// <summary>
/// Placeholder transcript so the panel can be judged at realistic density — bubble spacing, how a
/// long assistant paragraph wraps in a 400px column, where timestamps sit. Nothing here is real:
/// delete this file when the panel is wired to the Claude API and the transcript comes from the
/// server. Wording follows the house terminology (programme, variation read as V72, valuation
/// invoice) so the mock never teaches the wrong words.
/// </summary>
public static class ChatSampleTranscript
{
    public static IReadOnlyList<ChatMessage> Messages { get; } = new List<ChatMessage>
    {
        new(ChatAuthor.User,
            "Where has V72 got to?",
            "09:41"),
        new(ChatAuthor.Assistant,
            "V72 — Basement plant relocation, £48,320 — is Awaiting AI. It was issued to the "
            + "architect on 14 July, so it has been out eleven days.\n\n"
            + "Two bid packages branch off it. Both are priced and neither is committed, so the "
            + "cost is still an exposure rather than a liability.",
            "09:41"),
        new(ChatAuthor.User,
            "Is any of it in the last valuation invoice?",
            "09:44"),
        new(ChatAuthor.Assistant,
            "No. Nothing from V72 has been claimed yet — it cannot be until the AI comes back and "
            + "the variation is approved. The next valuation is dated 31 July, which gives the "
            + "architect six days if you want it in that claim.",
            "09:44"),
        new(ChatAuthor.User,
            "Draft a chaser to the architect for me.",
            "09:45")
    };
}
