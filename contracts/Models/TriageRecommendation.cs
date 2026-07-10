namespace Jewel.JPMS.Models;

// A date Claude spotted in the thread that matters for triage (a response deadline, a site visit,
// a JCT notice window). Date stays a string — it is display-only advice, never persisted.
public sealed record TriageRecommendationDate(string Date, string Meaning);

// Claude's recommendation for how to triage one email thread. Advisory only: the triager applies
// (or ignores) it through the same link/create/discard actions as always — nothing here writes.
// Action keys mirror the triage UI: link_to_existing, create_request, create_bid_package,
// tag_scheduling, create_todos, discard, none.
public sealed record TriageRecommendation(
    // False when the AI feature is unconfigured or the call failed — the UI hides the box.
    bool Available,
    string Summary,
    string RecommendedAction,
    // Suggested project — always validated server-side against the real project list, else null.
    string? ProjectId,
    string? SuggestedTitle,
    // Proposed to-do titles when the recommendation is create_todos.
    IReadOnlyList<string> TodoItems,
    // Other action keys worth considering (an email can carry several signals).
    IReadOnlyList<string> SecondaryActions,
    string Urgency,     // low | normal | high
    string Confidence,  // low | medium | high
    IReadOnlyList<TriageRecommendationDate> KeyDates,
    string Reasoning)
{
    public static TriageRecommendation Unavailable() => new(
        false, "", "none", null, null,
        Array.Empty<string>(), Array.Empty<string>(),
        "normal", "low", Array.Empty<TriageRecommendationDate>(), "");
}
